﻿namespace EnumerableTest.Runner

open System
open System.Diagnostics
open System.Reflection
open System.Threading
open Basis.Core
open EnumerableTest
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassType =
  let testMethodInfos (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && (m.GetParameters() |> Array.isEmpty)
        && m.ReturnType = typeof<seq<Test>>
      )

  let isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethodInfos |> Seq.isEmpty |> not

  let instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethodSchema =
  let ofMethodInfo (m: MethodInfo): TestMethodSchema =
    {
      MethodName                    = m.Name
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassPath =
  let ofFullName fullName =
    let namespacePath =
      fullName |> Str.splitBy "."
    let classPath =
      fullName |> Str.splitBy "." |> Seq.last |> Str.splitBy "+"
    {
      NamespacePath =
        namespacePath.[0..(namespacePath.Length - 2)]
      ClassPath =
        classPath.[0..(classPath.Length - 2)]
      Name =
        classPath.[classPath.Length - 1]
    }

  let ofType (typ: Type) =
    typ.FullName |> ofFullName

  let fullPath (this: TestClassPath) =
    [
      yield! this.NamespacePath
      yield! this.ClassPath
      yield this.Name
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassSchema =
  let ofType (typ: Type): TestClassSchema =
    {
      Path =
        typ |> TestClassPath.ofType
      TypeFullName                = typ.FullName
      Methods                     = 
        typ
        |> TestClassType.testMethodInfos
        |> Seq.map TestMethodSchema.ofMethodInfo
        |> Seq.toArray
    }

  let difference oldOne newOne =
    let d =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestMethodSchema).MethodName)
        (fun node -> (node: TestMethodSchema).MethodName)
        (oldOne: TestClassSchema).Methods
        (newOne: TestClassSchema).Methods
    let modified =
      d.Intersect |> Seq.map
        (fun (name, _, testMethodSchema) ->
          (name, testMethodSchema)
        )
      |> Map.ofSeq
    TestClassSchemaDifference.Create
      ( d.Right
      , d.Left
      , modified
      )

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module TestSuiteSchema =
  let difference oldOne newOne =
    let d =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassSchema).TypeFullName)
        (fun node -> (node: TestClassSchema).TypeFullName)
        (oldOne: TestSuiteSchema)
        (newOne: TestSuiteSchema)
    let modified =
      d.Intersect |> Seq.map
        (fun (name, l, r) ->
          (l.Path |> TestClassPath.fullPath, TestClassSchema.difference l r)
        )
      |> Map.ofSeq
    TestSuiteSchemaDifference.Create
      ( d.Right
      , d.Left
      , modified
      )

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethod =
  let ofResult name result disposingError duration =
    {
      MethodName                    = name
      Result                        = result
      DisposingError                = disposingError
      Duration                      = duration
    }

  let ofInstantiationError (e: exn) =
    let name = "default constructor"
    let result = SerializableGroupTest(name, [||], Some e)
    ofResult name result None TimeSpan.Zero

  /// Creates an instance of TestMethod
  /// by executing a test method of an instance and disposing the instance.
  let create (instance: TestInstance) (m: MethodInfo) =
    let stopwatch = Stopwatch.StartNew()
    let tests =
      m.Invoke(instance, [||]) :?> seq<Test>
    let groupTest =
      tests.ToTestGroup(m.Name)
    let disposingError =
      Option.tryCatch (fun () -> instance |> Disposable.dispose)
    let duration = stopwatch.Elapsed
    // Convert the result to be serializable.
    let groupTest =
      groupTest |> SerializableTest.ofGroupTest
    ofResult m.Name groupTest disposingError duration

  /// Builds computations to create TestMethod instance
  /// for each test method from a test class type.
  /// NOTE: Execute all computations to dispose created instances.
  let createManyAsync (typ: Type) =
    let methodInfos = typ |> TestClassType.testMethodInfos
    let instantiate = typ |> TestClassType.instantiate
    try
      let computations =
        methodInfos
        |> Seq.map
          (fun m ->
            let instance = instantiate ()
            let computation =
              async {
                return m |> create instance
              }
            (m, computation)
          )
        |> Seq.toArray
      (computations, None)
    with
    | e ->
      ([||], Some e)

  let isPassed (testMethod: TestMethod) =
    testMethod.Result.IsPassed
    && testMethod.DisposingError |> Option.isNone

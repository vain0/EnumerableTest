﻿namespace EnumerableTest.Runner.Wpf

open System
open System.Reflection
open System.Threading.Tasks
open EnumerableTest.Sdk
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethodResult =
  let create typ testMethod =
    {
      TypeFullName              = (typ: Type).FullName
      Method                    = testMethod
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let executeType (typ: Type) =
    let methodInfos = typ |> TestClassType.testMethodInfos
    let instantiate = typ |> TestClassType.instantiate
    try
      methodInfos
      |> Seq.map
        (fun m->
          let instance = instantiate ()
          async {
            let testMethod = m |> TestMethod.create instance
            return TestMethodResult.create typ testMethod
          }
        )
      |> Seq.toArray
    with
    | e ->
      let result = TestMethodResult.create typ (TestMethod.ofInstantiationError e)
      [| async { return result } |]

  let ofAssemblyAsObservable (assembly: Assembly) =
    let (types, asyncSeqSeq) =
      assembly.GetTypes()
      |> Seq.filter (fun typ -> typ |> TestClassType.isTestClass)
      |> Seq.map (fun typ -> (typ, typ |> executeType))
      |> Seq.toArray
      |> Array.unzip
    let (schema: TestSuiteSchema) =
      types
      |> Array.map TestClassSchema.ofType
    let observable =
      asyncSeqSeq
      |> Seq.collect id
      |> Observable.startParallel
    (schema, observable)

﻿namespace EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Type =
  open System
  open System.Collections
  open System.Collections.Generic
  open Basis.Core

  /// Represents a full name of a type.
  type FullName =
    {
      NamespacePath:
        array<string>
      TypePath:
        array<string>
      Name:
        string
    }
  with
    override this.ToString() =
      Array.append
        this.NamespacePath
        [|Array.append this.TypePath [|this.Name|] |> String.concat "+"|]
      |> String.concat "."

    static member Create(namespacePath, typePath, name) =
      {
        NamespacePath =
          namespacePath
        TypePath =
          typePath
        Name =
          name
      }

  [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
  module FullName =
    /// Creates an instance of FullName from a string.
    /// Format: Namespace1.Namespace2.Type1+Type2+TypeName.
    let ofString (fullName: string) =
      let (namespacePath, nestedTypeName) =
        fullName |> Str.splitBy "." |> Array.decomposeLast
      let (typePath, name) =
        nestedTypeName |> Str.splitBy "+" |> Array.decomposeLast
      FullName.Create(namespacePath, typePath, name)

    let fullPath (fullName: FullName) =
      [
        yield! fullName.NamespacePath
        yield! fullName.TypePath
        yield fullName.Name
      ]

  let fullName (typ: Type) =
    FullName.ofString typ.FullName

  let tryGetGenericTypeDefinition (this: Type) =
    if this.IsGenericType
      then this.GetGenericTypeDefinition() |> Some
      else None

  let interfaces (this: Type) =
    seq {
      if this.IsInterface then
        yield this
      yield! this.GetInterfaces()
    }

  let isCollectionType (this: Type) =
    seq {
      for ``interface`` in this |> interfaces do
        if ``interface`` = typeof<ICollection> then
          yield true
        else
          match ``interface`` |> tryGetGenericTypeDefinition with
          | Some genericInterface ->
            if genericInterface = typedefof<IReadOnlyCollection<_>>
              || genericInterface = typedefof<ICollection<_>>
              then yield true
          | None -> ()
    }
    |> Seq.exists id

  let isKeyValuePairType (this: Type) =
    this |> tryGetGenericTypeDefinition |> Option.exists ((=) typedefof<KeyValuePair<_, _>>)

  let tryMatchKeyedCollectionType (this: Type) =
    query {
      for ``interface`` in this |> interfaces do
      where (``interface``.IsGenericType)
      let genericInterface = ``interface``.GetGenericTypeDefinition()
      where
        (genericInterface = typedefof<IReadOnlyCollection<_>>
        || genericInterface = typedefof<ICollection<_>>)
      let elementType = ``interface``.GetGenericArguments().[0]
      where (elementType |> isKeyValuePairType)
      let types = elementType.GetGenericArguments()
      select (KeyValuePair<_, _>(types.[0], types.[1]))
    }
    |> Seq.tryHead

  let prettyName: Type -> string =
    let abbreviations =
      [
        (typeof<int>, "int")
        (typeof<int64>, "long")
        (typeof<float>, "double")
        (typeof<obj>, "object")
        (typeof<string>, "string")
      ] |> dict
    let rec prettyName (this: Type) =
      if this.IsGenericType then
        let name = this.Name |> Str.takeWhile ((<>) '`')
        let arguments = this.GenericTypeArguments |> Seq.map prettyName |> String.concat ", "
        sprintf "%s<%s>" name arguments
      else
        match abbreviations.TryGetValue(this) with
        | (true, name) -> name
        | (false, _) -> this.Name
    prettyName

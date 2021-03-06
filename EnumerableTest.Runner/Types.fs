﻿namespace EnumerableTest.Runner

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open Basis.Core
open EnumerableTest.Sdk

/// Represents an instance of a test class.
type TestInstance =
  obj

type TestMethodSchema =
  {
    MethodName: string
  }

type TestClassSchema =
  {
    TypeFullName:
      Type.FullName
    Methods:
      array<TestMethodSchema>
  }

type TestSuiteSchema =
  array<TestClassSchema>

type TestClassSchemaDifference =
  {
    Added:
      IReadOnlyList<TestMethodSchema>
    Removed:
      IReadOnlyList<TestMethodSchema>
    Modified:
      Map<string, TestMethodSchema>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

type TestSuiteSchemaDifference =
  {
    Added:
      IReadOnlyList<TestClassSchema>
    Removed:
      IReadOnlyList<TestClassSchema>
    Modified:
      Map<list<string>, TestClassSchemaDifference>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

type TestMethodResult =
  {
    MethodName:
      string
    Result:
      SerializableGroupTest
    DisposingError:
      option<MarshalValue>
    Duration:
      TimeSpan
  }
with
  member this.DisposingErrorOrNull =
    match this.DisposingError with
    | Some e -> e :> obj
    | None -> null

type TestResult =
  {
    TypeFullName:
      Type.FullName
    /// Represents a test method result or an instantiation error.
    Result:
      Result<TestMethodResult, exn>
  }

type TestClassResult =
  {
    TypeFullName:
      Type.FullName
    InstantiationError:
      option<Exception>
    TestMethodResults:
      array<TestMethodResult>
    NotCompletedMethods:
      array<TestMethodSchema>
  }

type TestSuite =
  IObservable<TestResult>

[<AbstractClass>]
type TestAssembly() =
  abstract TestCompleted: IObservable<TestResult>

  abstract Start: unit -> unit

  abstract Dispose: unit -> unit

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

type AssertionCount =
  {
    TotalCount:
      int
    ViolatedCount:
      int
    ErrorCount:
      int
    NotCompletedCount:
      int
  }

type TestStatistic =
  {
    AssertionCount:
      AssertionCount
    Duration:
      TimeSpan
  }

type TestStatus =
  | NotCompleted
  | Passed
  | Violated
  | Error

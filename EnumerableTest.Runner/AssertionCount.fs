﻿namespace EnumerableTest.Runner

open EnumerableTest.Sdk

type AssertionCount =
  {
    TotalCount                  : int
    ViolatedCount               : int
    ErrorCount                  : int
    NotCompletedCount           : int
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AssertionCount =
  let zero =
    {
      TotalCount                = 0
      ViolatedCount             = 0
      ErrorCount                = 0
      NotCompletedCount         = 0
    }

  let onePassed =
    {
      TotalCount                = 1
      ViolatedCount             = 0
      ErrorCount                = 0
      NotCompletedCount         = 0
    }

  let oneViolated =
    {
      TotalCount                = 1
      ViolatedCount             = 1
      ErrorCount                = 0
      NotCompletedCount         = 0
    }

  let oneError =
    {
      TotalCount                = 1
      ViolatedCount             = 0
      ErrorCount                = 1
      NotCompletedCount         = 0
    }

  let ofNotCompleted n =
    {
      TotalCount                = n
      ViolatedCount             = 0
      ErrorCount                = 0
      NotCompletedCount         = n
    }

  let add (l: AssertionCount) (r: AssertionCount) =
    {
      TotalCount                = l.TotalCount + r.TotalCount
      ViolatedCount             = l.ViolatedCount + r.ViolatedCount
      ErrorCount                = l.ErrorCount + r.ErrorCount
      NotCompletedCount         = l.NotCompletedCount + r.NotCompletedCount
    }

  let subtract (l: AssertionCount) (r: AssertionCount) =
    {
      TotalCount                = l.TotalCount - r.TotalCount
      ViolatedCount             = l.ViolatedCount - r.ViolatedCount
      ErrorCount                = l.ErrorCount - r.ErrorCount
      NotCompletedCount         = l.NotCompletedCount - r.NotCompletedCount
    }

  let groupSig =
    { new GroupSig<_>() with
        override this.Unit = zero
        override this.Multiply(l, r) = add l r
        override this.Divide(l, r) = subtract l r
    }

  let ofAssertion (assertion: Assertion) =
    if assertion.IsPassed
      then onePassed
      else oneViolated

  let ofGroupTest (groupTest: GroupTest) =
    seq {
      for assertion in groupTest.Assertions do
        yield assertion |> ofAssertion
      if groupTest.ExceptionOrNull |> isNull |> not then
        yield oneError
    }
    |> Seq.fold add zero

  let ofTestMethod (testMethod: TestMethod) =
    testMethod.Result |> ofGroupTest
    |> add
      (match testMethod.DisposingError with
      | Some _ -> oneError
      | None -> zero
      )

  let isPassed (count: AssertionCount) =
    count.ViolatedCount = 0 && count.ErrorCount = 0 && count.NotCompletedCount = 0

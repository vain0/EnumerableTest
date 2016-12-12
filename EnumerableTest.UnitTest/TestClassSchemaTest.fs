﻿namespace global
  type GlobalClass() =
    member this.PassingTest() =
      EnumerableTest.UnitTest.TestClass.passingTest

namespace EnumerableTest.UnitTest
  open Persimmon
  open Persimmon.Syntax.UseTestNameByReflection
  open EnumerableTest
  open EnumerableTest.Runner

  module TestClassPathTest =
    module test_ofType =
      let globalCase =
        test {
          let path = typeof<GlobalClass> |> TestClassPath.ofType
          do! path.NamespacePath |> assertEquals [||]
          do! path.ClassPath |> assertEquals [||]
          do! path.Name |> assertEquals "GlobalClass"
        }

      let nestedCase =
        test {
          let path = typeof<TestClass.Passing> |> TestClassPath.ofType 
          do! path.NamespacePath |> assertEquals [| "EnumerableTest"; "UnitTest" |]
          do! path.ClassPath |> assertEquals [| "TestClass" |]
          do! path.Name |> assertEquals "Passing"
        }

﻿namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Disposables
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

/// Represents a test class or a namespace.
[<Sealed>]
type FolderNode(name: string) =
  inherit TestTreeNode()

  let children = new ReactiveCollection<TestTreeNode>()

  let testStatistic =
    let accumulation = AccumulateBehavior.create TestStatistic.groupSig
    let subscriptions =
      children |> ReactiveCollection.mapAcquire
        (fun node -> accumulation.Add(node.TestStatistic))
    accumulation.Accumulation

  override this.Name = name

  override this.Children = children

  override this.TestStatistic = testStatistic

  override val IsExpanded =
    testStatistic |> ReactiveProperty.map (TestStatistic.isPassed >> not)
    :> IReadOnlyReactiveProperty<_>

  member this.FindOrAddFolderNode(path: list<string>) =
    match this.RouteOrFailure(path) with
    | Success node ->
      node
    | Failure (parent, name, path) ->
      let rec loop (parent: TestTreeNode) =
        function
        | [] ->
          parent
        | name :: path ->
          let node = FolderNode(name)
          parent.AddChild(node)
          loop node path
      loop parent (name :: path)

  static member CreateRoot() =
    FolderNode("")

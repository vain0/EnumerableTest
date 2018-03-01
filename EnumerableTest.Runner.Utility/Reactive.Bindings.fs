﻿namespace EnumerableTest.Runner

module ReactiveProperty =
  open System
  open System.Reactive.Linq
  open Reactive.Bindings

  let create x =
    new ReactiveProperty<_>(initialValue = x)

  let ofObservable initialValue (o: IObservable<'x>) =
    o.ToReactiveProperty(initialValue = initialValue)

  let map (f: 'x -> 'y) (this: IReadOnlyReactiveProperty<'x>) =
    this.Select(f).ToReactiveProperty()

  let collect (f: 'x -> IObservable<'y>) (rp: IReadOnlyReactiveProperty<'x>) =
    rp.SelectMany(f).ToReactiveProperty()

module ReactiveCommand =
  open System
  open Reactive.Bindings

  let create (canExecute: IReadOnlyReactiveProperty<_>) =
    new ReactiveCommand<_>(canExecuteSource = canExecute)

  let ofFunc (f: _ -> unit) canExecute =
    let it = create canExecute
    it.Subscribe(f) |> ignore<IDisposable>
    it

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReactiveCollection =
  open System
  open Reactive.Bindings

  let empty () =
    new ReactiveCollection<_>()

  let ofSeq xs =
    let list = new ReactiveCollection<_>()
    for x in xs do
      list.Add(x)
    list

  let mapAcquire f (list: ReactiveCollection<_>) =
    list.ToReadOnlyReactiveCollection(Func<_, _>(f), disposeElement = true)

namespace AB.Zettel

open System
open System.Collections.Generic
open System.Threading

open FSharp.Control

type TitleIndex() =
    let mutable store = Dictionary<DocumentPath, DocumentTitle>()
    let sem = new SemaphoreSlim(1, 1)
    let defaultTimeout = TimeSpan.FromMilliseconds 100.

    let criticalWorkflow (timeout: TimeSpan) wf = async {
        let! ct = Async.CancellationToken
        if sem.Wait(timeout, ct) then
            try
                do! wf
                return true
            finally
                sem.Release() |> ignore
        else
            return false
    }

    member __.Update(path, title, ?timeout) = async {
        let timeout = defaultTimeout |> defaultArg timeout
        let doStore = async { store.[path] <- title }
        return! criticalWorkflow timeout doStore
    }

    member __.Remove(path, ?timeout) = async {
        let timeout = defaultTimeout |> defaultArg timeout
        let doStore = async { store.Remove path |> ignore }
        return! criticalWorkflow timeout doStore
    }

    member __.Query(terms: #seq<string>, ?timeout) = async {
        let timeout = defaultTimeout |> defaultArg timeout
        let! ct = Async.CancellationToken
        let getSnapshot = async {
            if sem.Wait(timeout, ct) then
                try
                    return ResizeArray store |> Some
                finally
                    sem.Release() |> ignore
            else
                return None
        }

        // TODO: What's a good way to check for token cancellation in an expression-based function?
        let matchingTerms (DocumentTitle title) =
            terms
            |> Seq.filter (fun term -> title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            |> Seq.length
        let toMatch (path, title) =
            match matchingTerms title with
            | x when x > 0 -> Some (x, path, title)
            | _ -> None
        match! getSnapshot with
        | Some snapshot ->
            return snapshot
            |> Seq.map (fun kvp -> (kvp.Key, kvp.Value))
            |> Seq.choose toMatch
            |> Seq.sortByDescending (fun (q, _, _) -> q)
            |> Some
        | None -> return None
    }

    member __.Initialise(initialCache: AsyncSeq<(DocumentPath * DocumentTitle)>) = async {
        let! ct = Async.CancellationToken
        sem.Wait(ct)
        try
            let newStore = Dictionary<_,_>()
            for (path, title) in initialCache do
                newStore.[path] <- title
            ct.ThrowIfCancellationRequested()
            store <- newStore
        finally
            sem.Release() |> ignore
    }

    interface IDisposable with
        member __.Dispose() =
            sem.Dispose()

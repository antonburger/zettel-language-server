module TitleIndexTests

open AB.Zettel
open Xunit
open Swensen.Unquote

type ``Given an empty title index``() =
    let index = new TitleIndex()

    [<Fact>]
    member x.``Removing any path succeeds``() =
        Async.RunSynchronously <| index.Remove(DocumentPath "foo")

type ``Given an index with just this entry``() =
    let index = new TitleIndex()

    do
        Async.RunSynchronously <| async {
            do! index.Update(DocumentPath "foo", DocumentTitle "The complete collected works of Shakespeare") |> Async.Ignore
        }

    [<Fact>]
    member x.``Removing the single path gives an empty index``() =
        test <@ index
                |> TitleIndex.remove (DocumentPath "foo")
                |> TitleIndex.isEmpty @>

    [<Fact>]
    member x.``The following query matches the entry``() =
        let terms = [| "shakespeare" |]
        let documents = Async.RunSynchronously <| index.Query terms
        test <@ Option.isSome documents && (Option.get documents |> Seq.length) = 1 @>

type ``Given an index with these two entries``() =
    let index = new TitleIndex()

    do
        Async.RunSynchronously <| async {
            do! index.Update(DocumentPath "foo", DocumentTitle "A book about Shakespeare") |> Async.Ignore
            do! index.Update(DocumentPath "bar", DocumentTitle "A paper about Shakespeare") |> Async.Ignore
        }

    [<Fact>]
    let ``A query asking for books gives the book entry more weight``() =
        let terms = [| "book"; "shakespeare" |]
        let (_, DocumentPath path, DocumentTitle title) = index |> TitleIndex.query terms |> Seq.head
        test <@ (path, title) = ("foo", "A book about Shakespeare") @>

    [<Fact>]
    let ``A query asking for papers gives the paper entry more weight``() =
        let terms = [| "paper"; "shakespeare" |]
        let (_, DocumentPath path, DocumentTitle title) = index |> TitleIndex.query terms |> Seq.head
        test <@ (path, title) = ("bar", "A paper about Shakespeare") @>

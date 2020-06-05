module TitleIndexTests

open AB.Zettel
open Xunit
open Swensen.Unquote

type ``Given an empty title index``() =
    let index = TitleIndex.empty

    [<Fact>]
    member x.``Removing any path succeeds``() =
        TitleIndex.remove (DocumentPath "foo") index |> ignore

type ``Given an index with just this entry``() =
    let index =
        TitleIndex.empty
        |> TitleIndex.add (DocumentPath "foo") (DocumentTitle "The complete collected works of Shakespeare")

    [<Fact>]
    member x.``Removing the single path gives an empty index``() =
        test <@ index
                |> TitleIndex.remove (DocumentPath "foo")
                |> TitleIndex.isEmpty @>

    [<Fact>]
    member x.``The following query matches the entry``() =
        let terms = [| "shakespeare" |]
        let actual = index |> TitleIndex.query terms |> Seq.length
        test <@ actual = 1 @>

type ``Given an index with these two entries``() =
    let index =
        TitleIndex.empty
        |> TitleIndex.add (DocumentPath "foo") (DocumentTitle "A book about Shakespeare")
        |> TitleIndex.add (DocumentPath "bar") (DocumentTitle "A paper about Shakespeare")

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

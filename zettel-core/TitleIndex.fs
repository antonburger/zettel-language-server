namespace AB.Zettel

open System

type TitleIndex = private TitleIndex of Map<DocumentPath, DocumentTitle>

[<RequireQualifiedAccess>]
module TitleIndex =
    let empty = TitleIndex <| Map.empty

    let add path title (TitleIndex map) =
        map |> Map.add path title |> TitleIndex

    let remove path (TitleIndex map) =
        map |> Map.remove path |> TitleIndex

    let isEmpty (TitleIndex map) =
        Map.isEmpty map

    let query (terms: #seq<string>) (TitleIndex map) =
        let matchingTerms (DocumentTitle title) =
            terms
            |> Seq.filter (fun term -> title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            |> Seq.length
        let toMatch (path, title) =
            match matchingTerms title with
            | x when x > 0 -> Some (x, path, title)
            | _ -> None
        map
        |> Map.toSeq
        |> Seq.choose toMatch
        |> Seq.sortByDescending (fun (q, _, _) -> q)

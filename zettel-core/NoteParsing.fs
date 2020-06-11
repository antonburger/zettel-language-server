namespace AB.Zettel

type NoteMetadata =
    { Title: string option }

type ParseError =
    | ParseException of exn

type CompletionRange =
    { replacementStartCol: int
      replacementLength: int
      completionQuery: string }

module NoteMetadata =
    open System.IO
    open YamlDotNet.Core
    open YamlDotNet.Core.Events
    open YamlDotNet.Serialization
    open YamlDotNet.Serialization.NamingConventions

    type NoteMetadataDto() =
        member val public Title = "" with get, set
        member x.ToNote() =
            { Title = if System.String.IsNullOrWhiteSpace(x.Title) then None else Some x.Title }

    let private deserializer =
        DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()

    let parse (reader: TextReader) =
        let parser = Parser(reader)
        parser.Consume<StreamStart>() |> ignore
        parser.Consume<DocumentStart>() |> ignore
        let noteMetadata = deserializer.Deserialize<NoteMetadataDto>(parser)
        parser.Consume<DocumentEnd>() |> ignore
        noteMetadata.ToNote()

module Completion =
    open System
    open System.Text.RegularExpressions

    let private linkPattern = Regex(@"
        \G
        \[ [^]]*? \]
        (
           \( [^)]*? \)
        |
           \[ [^]]*? \]
        )?", RegexOptions.Compiled ||| RegexOptions.IgnorePatternWhitespace)

    let getLinkExtent (line: string) startAt =
        let m = linkPattern.Match(line, startAt)
        if m.Success
        then Some m.Length
        else None

    let (|InsideAnyLink|_|) (line: string, cursorIndex) =
        if cursorIndex = 0 then None
        else
            match line.LastIndexOfAny([| '['; ']' |], cursorIndex - 1) with
            | index when index < 0 -> None
            | index when line.[index] = '[' -> Some index
            | _ -> None

    let (|InsideLink|_|) (line, cursorIndex) =
        match (line, cursorIndex) with
        | InsideAnyLink index ->
            Some {
                replacementStartCol = index
                replacementLength =
                    match getLinkExtent line index with
                    | Some length -> length
                    | None -> cursorIndex - index
                completionQuery = line.Substring(index + 1, cursorIndex - (index + 1))
            }
        | _ -> None

    let (|InsideImageLink|_|) (line, cursorIndex) =
        match (line, cursorIndex) with
        | InsideAnyLink index when index > 0 && line.[index - 1] = '!' -> Some ()
        | _ -> None

    let (|OutsideLink|) (line: string, cursorIndex) =
        let rangeStart, rangeLength, query =
            if cursorIndex = 0
            then 0, 0, ""
            else
                match line.LastIndexOf(' ', cursorIndex - 1) with
                | index when index < 0 -> 0, cursorIndex, line.Substring(0, cursorIndex)
                | index -> index + 1, cursorIndex - (index + 1), line.Substring(index + 1, cursorIndex - (index + 1))
        {
            replacementStartCol = rangeStart
            replacementLength = rangeLength
            completionQuery = query
        }

    let getCompletionRange line cursorIndex =
        match line, cursorIndex with
        | InsideImageLink -> None
        | InsideLink range
        | OutsideLink range -> Some range

    // TODO: use Span?
    let getLines (text: string) =
        text.Split([| "\r\n"; "\r"; "\n" |], StringSplitOptions.None)

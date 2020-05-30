namespace AB.Zettel

type NoteMetadata =
    { Title: string option }

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

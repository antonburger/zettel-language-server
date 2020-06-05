namespace AB.Zettel

type Note = {
    RelativePath: string
    Title: string option
}

type ZettelKasten = {
    Notes: Note list
}

module ZettelKasten =
    open System
    open System.IO
    open NoteMetadata
(*
    let enumerateMarkdownFilesRelative (rootDir: string) = seq {
        let ensureTrailingBackslash (path: string) =
            if path.EndsWith("/") || path.EndsWith("\\") then path else path + "/"
        let rootUri = rootDir |> ensureTrailingBackslash |> Uri
        for path in Directory.EnumerateFiles(rootDir, "*.md", SearchOption.AllDirectories) do
            let targetUri = path |> Uri
            rootUri.MakeRelativeUri(targetUri).OriginalString
    }

    let build (rootDir: string) =
        let files = enumerateMarkdownFilesRelative rootDir
        async {
            let notes = seq {
                for file in files do
                    use reader = new StreamReader(Path.Combine(rootDir, file))
                    // TODO: what do we do with individual note errors?
                    match! NoteMetadata.parse reader with
                    | Ok metadata ->
                        Some { RelativePath = file
                          Title = metadata.Title }
                    | _ -> None

                }
            { Notes = Seq.choose parse files }
        }
*)

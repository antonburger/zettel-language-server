// Learn more about F# at http://fsharp.org

open System
open System.IO

open FSharp.Data

open LSP
open LSP.Log
open LSP.Types

open AB.Zettel

type IndexMessage =
| Initialise of rootPath: string
| UpdateFile of filePath: string
| RemoveFile of filePath: string
| Query of string * AsyncReplyChannel<(DocumentPath * DocumentTitle) list>

type IndexState<'TIndex> =
| Uninitialised
| Initialised of 'TIndex

let enumerateMarkdownFiles (rootDir: string) =
    Directory.EnumerateFiles(rootDir, "*.md", SearchOption.AllDirectories)

let titleIndex = MailboxProcessor.Start(fun inbox ->
    let parseTitle (path: string) =
        dprintfn "Indexing title for %s" path
        // TODO: need to handle IO and parsing errors
        use reader = new StreamReader(path)
        match NoteMetadata.parse reader with
        | { Title = Some title } -> Some <| (DocumentPath path, DocumentTitle title)
        | _ -> None
    let rec loop state = async {
        let! msg = inbox.Receive()
        match state, msg with
        | _, Initialise rootPath ->
            dprintfn "Building title index for %s" rootPath
            let files = enumerateMarkdownFiles rootPath
            let index =
                files
                |> Seq.choose parseTitle
                |> Seq.fold (fun index (path, title) -> TitleIndex.add path title index) TitleIndex.empty
            dprintfn "Index built"
            return! loop (Initialised index)
        | Uninitialised, _ ->
            return! loop Uninitialised
        | Initialised index, UpdateFile path ->
            let index =
                match parseTitle path with
                | Some (path, title) -> TitleIndex.add path title index
                | _ -> index
            return! loop (Initialised index)
        | Initialised index, RemoveFile path ->
            let index = index |> TitleIndex.remove (DocumentPath path)
            return! loop (Initialised index)
        | Initialised index, Query (query, replyChannel) ->
            dprintfn "Got a title query for %s" query
            let terms = query.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
            let results = index |> TitleIndex.query terms |> Seq.truncate 10 |> Seq.map(fun (_, p, t) -> (p, t)) |> Seq.toList
            replyChannel.Reply results
            return! loop (Initialised index)
    }
    loop Uninitialised
)

let makeRelative rootDir =
    let ensureTrailingBackslash (path: string) =
        if path.EndsWith("/") || path.EndsWith("\\") then path else path + "/"
    let rootUri = rootDir |> ensureTrailingBackslash |> Uri
    fun targetPath ->
        let targetUri = targetPath |> Uri
        rootUri.MakeRelativeUri(targetUri).OriginalString

let asyncNoOp = async { () }

type ZettelLanguageServer(client: ILanguageClient) =
    let mutable rootDir: string option = None

    interface ILanguageServer with
        member this.Initialize(p) =
            let fixPath (uriLocalPath: string) =
                if uriLocalPath.StartsWith "/"
                then uriLocalPath.Substring 1
                else uriLocalPath
            async {
                match p.rootUri with
                | Some rootUri ->
                    dprintfn "Initializing for %A" rootUri
                    rootDir <- rootUri.LocalPath |> fixPath |> Some
                    rootDir
                    |> Option.iter (Initialise >> titleIndex.Post)
                | _ -> dprintfn "No root URI in Initialize message: %A" p
                return {
                    capabilities =
                        { defaultServerCapabilities with
                              completionProvider = Some {
                                  resolveProvider = false
                                  triggerCharacters = [] }}
                }
            }

        member this.DidChangeConfiguration(_) = asyncNoOp
        member this.DidChangeTextDocument(_) = asyncNoOp
        member this.DidChangeWatchedFiles(_) = asyncNoOp
        member this.DidChangeWorkspaceFolders(_) = asyncNoOp
        member this.DidCloseTextDocument(_) = asyncNoOp
        member this.DidOpenTextDocument(_) = asyncNoOp
        member this.DidSaveTextDocument(_) = asyncNoOp
        member this.WillSaveTextDocument(_) = asyncNoOp
        member this.Initialized() = asyncNoOp
        member this.Shutdown() = asyncNoOp
        member this.ExecuteCommand(_) = asyncNoOp

        member this.CodeActions(arg1: CodeActionParams): Async<Command list> =
            failwith "Not Implemented"
        member this.CodeLens(arg1: CodeLensParams): Async<CodeLens list> =
            failwith "Not Implemented"
        member this.Completion(arg1: TextDocumentPositionParams): Async<CompletionList option> = async {
            let query = "tools"
            try
                let documents = titleIndex.PostAndReply((fun replyChannel -> Query (query, replyChannel)), 5000)
                return Some {
                    isIncomplete = false
                    items =
                        documents
                        |> List.map (fun (DocumentPath path, DocumentTitle title) -> {
                            label = title
                            kind = Some CompletionItemKind.File
                            detail = sprintf "[%s](%s)" title path |> Some
                            documentation = None
                            sortText = None
                            filterText = None
                            insertText = sprintf "[%s](%s)" title path |> Some
                            insertTextFormat = Some InsertTextFormat.PlainText
                            textEdit = None
                            additionalTextEdits = []
                            commitCharacters = []
                            command = None
                            data = JsonValue.Null
                        })
                }
            with
            | :? OperationCanceledException -> return None

        }
        member this.DocumentFormatting(arg1: DocumentFormattingParams): Async<TextEdit list> =
            failwith "Not Implemented"
        member this.DocumentHighlight(arg1: TextDocumentPositionParams): Async<DocumentHighlight list> =
            failwith "Not Implemented"
        member this.DocumentLink(arg1: DocumentLinkParams): Async<DocumentLink list> =
            failwith "Not Implemented"
        member this.DocumentOnTypeFormatting(arg1: DocumentOnTypeFormattingParams): Async<TextEdit list> =
            failwith "Not Implemented"
        member this.DocumentRangeFormatting(arg1: DocumentRangeFormattingParams): Async<TextEdit list> =
            failwith "Not Implemented"
        member this.DocumentSymbols(arg1: DocumentSymbolParams): Async<SymbolInformation list> =
            failwith "Not Implemented"
        member this.FindReferences(arg1: ReferenceParams): Async<Location list> =
            failwith "Not Implemented"
        member this.GotoDefinition(arg1: TextDocumentPositionParams): Async<Location list> =
            failwith "Not Implemented"
        member this.Hover(arg1: TextDocumentPositionParams): Async<Hover option> =
            failwith "Not Implemented"
        member this.Rename(arg1: RenameParams): Async<WorkspaceEdit> =
            failwith "Not Implemented"
        member this.ResolveCodeLens(arg1: CodeLens): Async<CodeLens> =
            failwith "Not Implemented"
        member this.ResolveCompletionItem(arg1: CompletionItem): Async<CompletionItem> =
            failwith "Not Implemented"
        member this.ResolveDocumentLink(p) =
            failwith "Not Implemented"
        member this.SignatureHelp(arg1: TextDocumentPositionParams): Async<SignatureHelp option> =
            failwith "Not Implemented"
        member this.WillSaveWaitUntilTextDocument(arg1: WillSaveTextDocumentParams): Async<TextEdit list> =
            failwith "Not Implemented"
        member this.WorkspaceSymbols(arg1: WorkspaceSymbolParams): Async<SymbolInformation list> =
            failwith "Not Implemented"

[<EntryPoint>]
let main argv =
    use read = new BinaryReader(Console.OpenStandardInput())
    use write = new BinaryWriter(Console.OpenStandardOutput())
    let serverFactory(client) = ZettelLanguageServer(client) :> ILanguageServer
    dprintfn "Listening on stdin"
    try
        LanguageServer.connect(serverFactory, read, write)
        0
    with e ->
        dprintfn "Exception in zettelkasten language server %O" e
        1


open System
open System.IO

open FSharp.Control
open FSharp.Data

open LSP
open LSP.Log
open LSP.Types

open AB.Zettel

let enumerateMarkdownFiles (rootDir: string) =
    Directory.EnumerateFiles(rootDir, "*.md", SearchOption.AllDirectories)

let makeRelative rootDir =
    let ensureTrailingBackslash (path: string) =
        if path.EndsWith("/") || path.EndsWith("\\") then path else path + "/"
    let rootUri = rootDir |> ensureTrailingBackslash |> Uri
    fun targetPath ->
        let targetUri = targetPath |> Uri
        rootUri.MakeRelativeUri(targetUri).OriginalString

let asyncNoOp = async { () }

let getTitles rootDir = asyncSeq {
    let parseTitle (path: string) =
        dprintfn "Indexing title for %s" path
        // TODO: need to handle IO and parsing errors
        use reader = new StreamReader(path)
        match NoteMetadata.parse reader with
        | { Title = Some title } -> Some <| (DocumentPath path, DocumentTitle title)
        | _ -> None
    for file in enumerateMarkdownFiles rootDir do
        match parseTitle file with
        | Some pair -> yield pair
        | None -> ()
}

type ZettelLanguageServer(client: ILanguageClient) =
    let docs = DocumentStore()
    let titleIndex = new TitleIndex()
    let mutable rootDir: string option = None

    interface ILanguageServer with
        member this.Initialize(p) =
            async {
                match p.rootUri with
                | Some rootUri ->
                    dprintfn "Initializing for %A" rootUri
                    // TODO: Do this initialisation on a separate thread
                    do! rootUri.LocalPath |> getTitles |> titleIndex.Initialise
                | _ -> dprintfn "No root URI in Initialize message: %A" p
                return {
                    capabilities =
                        { defaultServerCapabilities with
                            completionProvider = Some { resolveProvider = false; triggerCharacters = [] }
                            textDocumentSync =
                                { defaultTextDocumentSyncOptions with
                                    openClose = true
                                    change = TextDocumentSyncKind.Incremental
                                }
                        }
                }
            }

        member this.DidOpenTextDocument(p) = async {
            docs.Open(p)
        }

        member this.DidCloseTextDocument(p) = async {
            docs.Close(p)
        }

        member this.DidChangeTextDocument(p) = async {
            docs.Change(p)
        }

        member this.DidChangeWatchedFiles(p) = async {
            ()
        }

        member this.DidChangeConfiguration(_) = asyncNoOp
        member this.DidChangeWorkspaceFolders(_) = asyncNoOp
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
            match! titleIndex.Query([| query |]) with
            | Some documents ->
                return Some {
                    isIncomplete = false
                    items =
                        documents
                        |> Seq.map (fun (_, DocumentPath path, DocumentTitle title) -> {
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
                        |> Seq.toList
                }
            | None -> return None
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

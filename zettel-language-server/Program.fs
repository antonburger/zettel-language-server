// Learn more about F# at http://fsharp.org

open System
open System.IO
open LSP
open LSP.Log
open LSP.Types

let asyncNoOp = async { () }

type ZettelLanguageServer(client: ILanguageClient) =
    interface ILanguageServer with
        member this.Initialize(p) =
            let fixPath (uriLocalPath: string) =
                if uriLocalPath.StartsWith "/"
                then uriLocalPath.Substring 1
                else uriLocalPath
            async {
                match p.rootUri with
                | Some rootUri -> dprintfn "Initializing for %A" rootUri
                | _ -> dprintfn "No root URI in Initialize message: %A" p
                return {
                    capabilities = defaultServerCapabilities
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
        member this.Completion(arg1: TextDocumentPositionParams): Async<CompletionList option> =
            failwith "Not Implemented"
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

namespace AB.Zettel

[<Struct>]
type DocumentPath = DocumentPath of string
[<Struct>]
type DocumentTitle = DocumentTitle of string

type DocumentChange =
    | DocumentContentChange of newContent: string
    | DocumentDeleted

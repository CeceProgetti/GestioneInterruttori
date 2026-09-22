Imports Microsoft.Data.Sqlite

Namespace Data
    ''' <summary>
    ''' Punto unico di accesso al database Interruttori.sqlite. Il percorso del file
    ''' viene fornito dall'applicazione ospitante, che conosce la posizione dei db.
    ''' </summary>
    Public Class InterruttoriDbContext
        Private ReadOnly _connectionString As String

        Public Sub New(percorsoDatabase As String)
            _connectionString = $"Data Source={percorsoDatabase}"
        End Sub

        Public Function CreaConnessione() As SqliteConnection
            Dim connessione As New SqliteConnection(_connectionString)
            connessione.Open()
            Return connessione
        End Function
    End Class
End Namespace

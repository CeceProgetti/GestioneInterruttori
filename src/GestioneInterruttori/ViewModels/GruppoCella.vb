Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' Un'assegnazione: una Linea prodotto + un insieme di Taglie a cui vengono dati gli stessi
    ''' valori di cella. Alla conferma, ogni gruppo genera una riga ConfigurazioneCella per ogni
    ''' Taglia al suo interno.
    ''' </summary>
    Public Class GruppoCella
        Public Property LineaProdotto As LineaProdotto
        Public Property Taglie As List(Of String)

        Public Property AltezzaCella As Double?
        Public Property LarghezzaCella As Double?
        Public Property AltezzaCellaVerticale As Double?
        Public Property LarghezzaCellaVerticale As Double?
        Public Property ProfonditaCella As Double?

        Public ReadOnly Property TaglieDescrizione As String
            Get
                Return String.Join(", ", Taglie)
            End Get
        End Property
    End Class
End Namespace

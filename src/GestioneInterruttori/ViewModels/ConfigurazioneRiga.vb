Namespace ViewModels
    ''' <summary>
    ''' Riga della griglia configurazioni: una configurazione applicabile all'interruttore
    ''' selezionato, coi valori di default già impostati e modificabili dall'utente.
    ''' </summary>
    Public Class ConfigurazioneRiga
        Public Property IdConfigurazione As Integer
        Public Property Nome As String
        Public Property DeltaH As Double?
        Public Property DeltaL As Double?
        Public Property DeltaP As Double?
    End Class
End Namespace

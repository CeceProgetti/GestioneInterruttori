Imports System.Collections.ObjectModel

Namespace ViewModels
    ''' <summary>Le opzioni di uno stesso Tipo (es. Esecuzione), tra cui se ne sceglie una sola.</summary>
    Public Class GruppoConfigurazione
        Public Property Tipo As String
        Public Property Opzioni As ObservableCollection(Of ConfigurazioneAssegnabile)
    End Class
End Namespace

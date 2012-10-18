Imports DDDSample

Public Class CustomerAggregate
    Public ReadOnly Changes As IList(Of IEvent) = New List(Of IEvent)
    ReadOnly _state As CustomerState

    Public Sub New(events As IEnumerable(Of IEvent))
        _state = New CustomerState(events)
    End Sub
    Public Sub Create(id As CustomerId, name As String)
        If _state.Created Then Throw New InvalidOperationException("Customer Already Created")


    End Sub

    Public Sub Apply(e As IEvent)
        _state.Mutate(e)
        Changes.Add(e)
    End Sub
End Class

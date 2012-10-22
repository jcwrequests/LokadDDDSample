Imports DDDSample

Public Class CustomerApplicationService
    Implements IApplicationService

    ReadOnly _eventStore As IEventStore

    Public Sub New(eventStore As IEventStore)
        If eventStore Is Nothing Then Throw New ArgumentNullException("eventStore")
        _eventStore = eventStore
    End Sub


    Public Sub Execute(command As ICommand) Implements IApplicationService.Execute
        [When](command)
    End Sub

    Public Sub [When](command As CreateCustomer)
        Update(command.Id, Sub(a) a.Create(command.Id, command.CustomerName))
    End Sub
    Public Sub Update(customerID As CustomerId, execute As Action(Of CustomerAggregate))
        While True
            Dim eventStream = _eventStore.LoadEventStream(id:=customerID)
            Dim customer As New CustomerAggregate(eventStream.Events)
            execute(customer)

            Try
                _eventStore.AppendEventsToStream(id:=customerID,
                                           version:=eventStream.StreamVersion,
                                           events:=customer.Changes)

            Catch ex As OptimisticConcurrencyException
                For Each customerEvent In customer.Changes
                    For Each actutalEvent In ex.ActualEvents
                        If ConflictsWith(customerEvent, actutalEvent) Then
                            Dim msg = String.Format("Conflict between {0} and {1}", customerEvent, actutalEvent)
                            Throw New RealConcurrencyException(msg, ex)
                        End If
                    Next
                Next
            End Try
        End While
    End Sub
    Public Function ConflictsWith(x As IEvent, y As IEvent)
        Return x.GetType.Equals(y.GetType)
    End Function
End Class

# CANsharp (previously CANdb Reader)

- ByteOrder.cs
-- enum of either Intel (little Indian) or Motorola (big Indian)
- CANdb.cs
-- manages all relationship between signal and message
- CANtransmitter.cs
-- based on sample included in Vector XL Driver Library, provides method to check connectivity to CAN bus and tranmit message
- Message.cs
-- Message class
- Parser.cs
-- give string of path to candb file and it creates CANdb instance
- Signal.cs
-- Signal class
- UserControl1.xaml (.cs)
-- does NOTHING

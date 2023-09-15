using System;

[AttributeUsage(AttributeTargets.Struct)]
internal class SendPacketAttribute : Attribute
{
    public int packetID { get; set; }
}
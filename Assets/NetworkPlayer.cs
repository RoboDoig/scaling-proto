using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;

public class NetworkPlayer : IDarkRiftSerializable
{
    public ushort id {get; set;}
    public string playerName {get; set;}
    public bool isHost {get; set;}

    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public byte colorR { get; set; }
    public byte colorG { get; set; }
    public byte colorB { get; set; }

    public NetworkPlayer() {
        
    }

    public NetworkPlayer(ushort _id, string _playerName, bool _isHost, float _x, float _y, float _z, byte _colorR, byte _colorG, byte _colorB)
    {
        id = _id;
        playerName = _playerName;
        isHost = _isHost;
        x = _x;
        y = _y;
        z = _z;
        colorR = _colorR;
        colorG = _colorG;
        colorB = _colorB;
    }

    public void Deserialize(DeserializeEvent e) {
        id = e.Reader.ReadUInt16();
        playerName = e.Reader.ReadString();
        isHost = e.Reader.ReadBoolean();
        x = e.Reader.ReadSingle();
        y = e.Reader.ReadSingle();
        z = e.Reader.ReadSingle();
        colorR = e.Reader.ReadByte();
        colorG = e.Reader.ReadByte();
        colorB = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(id);
        e.Writer.Write(playerName);
        e.Writer.Write(isHost);
        e.Writer.Write(x);
        e.Writer.Write(y);
        e.Writer.Write(z);
        e.Writer.Write(colorR);
        e.Writer.Write(colorG);
        e.Writer.Write(colorB);
    }
}

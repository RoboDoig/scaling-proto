using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleNetworkEntity
{
    public ushort networkID;
    public string playerName;

    public SimpleNetworkEntity(ushort _networkID, string _playerName) {
        networkID = _networkID;
        playerName = _playerName;
    }
}

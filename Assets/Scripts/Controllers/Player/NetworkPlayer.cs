using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private List<UnityEngine.Object> toRemove;
    [SerializeField] private CharacterController charController;
    [SerializeField] private CharacterAnimator characterAnimator;
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private Transform _renderer;
    [SerializeField] public Text playerName;
    [Space]
    [SerializeField] private float smoothingTime;
    private int packetSize;

    [Space]
    [SerializeField] public int maxLife;
    private int defense = 0;
    public int life = 0;
    

    private uint tick = 0;
    private uint previousTick = 0;
    private Packet[] packet;
    private bool firstTime = true;

    private Queue<Packet> packetQueue = new Queue<Packet>();
    Packet lastEnqueued;

    private void Awake()
    {
        packetSize = GameManager.instance.packetSize;
        _camera = Camera.main;
        packet = new Packet[packetSize];
        UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera ppcam = _camera.GetComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>();
        ppcam.refResolutionX = GameManager.instance.screen.x / 2;
        ppcam.refResolutionY = GameManager.instance.screen.y / 2;
    }

    public override void OnNetworkSpawn()
    {
        gameObject.name = "Player " + GetComponent<NetworkObject>().OwnerClientId;
        GameManager.instance.players[GetComponent<NetworkObject>().OwnerClientId] = transform;
        GameManager.instance.front = transform.GetChild(1);
        if (!IsOwner)
        {
            Destroy(charController);
            foreach (UnityEngine.Object obj in toRemove)
            {
                Destroy(obj);
            }
        }
        else
        {
            playerName.text = GameManager.instance.playerName;
            GameManager.instance.SendPlayerNameServerRpc((byte)GameManager.instance.playerId, playerName.text);
            EnemyManager.instance.pgm.target = transform;
            _camera.GetComponent<CameraSnapping>().player = transform;
            GameManager.instance.playersInitialized.Invoke();
        }
    }


    private void UpdateState(Packet[] packet, uint _tick)
    {
        if (firstTime)
        {
            previousTick = _tick;

            tick = previousTick - (uint)packetSize;

            for (int i = 0; i < packetSize; i++)
            {
                packetQueue.Enqueue(packet[i]);
                lastEnqueued = packet[i];
            }

            firstTime = false;
        }

        if (_tick <= previousTick) return;

        if (_tick - previousTick > packetSize)
        {
            for (int i = (int)previousTick + packetSize; i < (int)_tick; i++)
            {
                packetQueue.Enqueue(lastEnqueued);
            }
        }

        for (int i = 0; i < packetSize; i++)
        {
            packetQueue.Enqueue(packet[i]);
            lastEnqueued = packet[i];
        }

        previousTick = _tick;
    }


    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
    private void SendPacketServerRpc(Packet[] packet, uint _tick)
    {
        SendPacketClientRpc(packet, _tick);
        UpdateState(packet, _tick);
    }


    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void SendPacketClientRpc(Packet[] packet, uint _tick)
    {
        UpdateState(packet, _tick);
    }

    
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            OwnerLogic();
        }
        else
        {
            ClientLogic();
        }

        tick++;
    }


    private void OwnerLogic()
    {
        if (tick % packetSize == 0 && tick != 0)
        {
            if (IsHost)
            {
                SendPacketClientRpc(packet, tick);
            }
            else
            {
                SendPacketServerRpc(packet, tick);
            }
        }

        packet[tick % packetSize] = new Packet()
        {
            Pos = player.position,
            Vel = charController.direction
        };

        characterAnimator.direction = charController.direction;
    }


    Vector2 posVel;
    private void ClientLogic()
    {
        if (firstTime) return;

        if (packetQueue.Count <= 1)
        {
            packetQueue.Enqueue(lastEnqueued);
            tick--;
        }

        if (previousTick - tick > 3 * packetSize)
        {
            for (int i = 0; i < previousTick - tick - packetSize; i++)
            {
                packetQueue.Dequeue();

            }

            tick = previousTick - (uint)packetSize;
        }

        Packet currentState = packetQueue.Dequeue();

        player.position = Vector2.SmoothDamp(player.position, currentState.Pos, ref posVel, smoothingTime);
        characterAnimator.direction = currentState.Vel;
    }


    private struct Packet : INetworkSerializable
    {
        float posX;
        float posY;
        float velX;
        float velY;

        internal Vector2 Pos
        {
            get 
            { 
                return new Vector2(posX, posY);
            }

            set
            {
                posX = value.x;
                posY = value.y;
            }
        }

        internal Vector2 Vel
        {
            get
            {
                return new Vector2(velX, velY);
            }

            set
            {
                velX = value.x;
                velY = value.y;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref velX);
            serializer.SerializeValue(ref velY);
        }
    }
}
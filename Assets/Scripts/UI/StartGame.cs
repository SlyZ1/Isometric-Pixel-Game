using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class StartGame : MonoBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject buttons;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_InputField name;
    [SerializeField] private int maxPlayer;
    [SerializeField] private UnityTransport transport;


    private async void Awake()
    {
        buttons.SetActive(false);

        await Authenticate();

        buttons.SetActive(true);
    }


    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateGame()
    {
        try
        {
            if (name.text.Replace(" ", "") == "" || name.text == null)
            {
                Debug.LogError("Name is non-valid");
                return;
            }

            GameManager.instance.playerName = name.text;

            Allocation a = await RelayService.Instance.CreateAllocationAsync(maxPlayer, "europe-west2");

            List<Region> region = await Relay.Instance.ListRegionsAsync();

            Debug.Log(await RelayService.Instance.GetJoinCodeAsync(a.AllocationId));

            RelayServerData relayServerData = new RelayServerData(a, "udp");

            transport.SetRelayServerData(relayServerData);

            buttons.SetActive(false);
            UI.SetActive(true);

            NetworkManager.Singleton.StartHost();

            gameObject.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinGame()
    {
        Debug.Log("Trying to join...");
        try
        {
            if (name.text.Replace(" ", "") == "" || name.text == null)
            {
                Debug.LogError("Name is non-valid");
                return;
            }

            GameManager.instance.playerName = name.text;

            JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(input.text);

            RelayServerData relayServerData = new RelayServerData(a, "udp");

            transport.SetRelayServerData(relayServerData);

            buttons.SetActive(false);
            UI.SetActive(true);

            NetworkManager.Singleton.StartClient();

            gameObject.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}

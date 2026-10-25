using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("Paneles del Lobby")]
    public GameObject panelOpciones;
    public GameObject panelEspera;

    [Header("Elementos de Entrada / Salida")]
    public TMP_InputField inputCodigoRoom;
    public TMP_Text txtCodigoSala;
    public TMP_Text txtEstadoJugadores;
    public Button btnIniciarPartida;

    [Header("Configuración de Nombres de Escena")]
    public string mainMenuSceneName = "MainMenu";
    public string gameARSceneName = "SampleScene";

    void Start()
    {
        // Sincroniza la carga de escena automáticamente entre anfitrión y clientes
        PhotonNetwork.AutomaticallySyncScene = true;

        // Conectar a la nube de Photon si no está conectado
        if (!PhotonNetwork.IsConnected)
        {
            txtEstadoJugadores.text = "Conectando al servidor...";
            PhotonNetwork.ConnectUsingSettings();
        }

        MostrarPanelOpciones();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Conectado al servidor Maestro.");
        PhotonNetwork.JoinLobby();
    }

    public void MostrarPanelOpciones()
    {
        panelOpciones.SetActive(true);
        panelEspera.SetActive(false);
    }

    // --- MÉTODOS DE BOTONES ---

    // Crear partida multijugador
    public void OnClickCrearPartida()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        string codigoSala = "TNK" + Random.Range(100, 999);
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };

        txtEstadoJugadores.text = "Creando sala...";
        PhotonNetwork.CreateRoom(codigoSala, roomOptions);
    }

    // Unirse a partida con el código del InputField
    public void OnClickUnirsePartida()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        string codigoIngresado = inputCodigoRoom.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(codigoIngresado))
        {
            Debug.LogWarning("[Lobby] Por favor ingresa un código de sala.");
            return;
        }

        txtEstadoJugadores.text = "Uniéndose a la sala...";
        PhotonNetwork.JoinRoom(codigoIngresado);
    }

    // Iniciar el juego en red (solo Anfitrión)
    public void OnClickIniciarPartida()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameARSceneName);
        }
    }

    // Salir de la sala actual
    public void OnClickSalirSala()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        MostrarPanelOpciones();
    }

    // Regresar al menú principal
    public void OnClickVolverAlMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- CALLBACKS AUTOMÁTICOS DE PHOTON ---

    public override void OnJoinedRoom()
    {
        panelOpciones.SetActive(false);
        panelEspera.SetActive(true);

        txtCodigoSala.text = "CÓDIGO: " + PhotonNetwork.CurrentRoom.Name;
        ActualizarEstadoJugadores();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ActualizarEstadoJugadores();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarEstadoJugadores();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // Si el código ya existía, vuelve a intentar generar uno
        OnClickCrearPartida();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        MostrarPanelOpciones();
        Debug.LogError("[Photon] Error al unirse a la sala: " + message);
    }

    private void ActualizarEstadoJugadores()
    {
        int totalJugadores = PhotonNetwork.CurrentRoom.PlayerCount;

        // Obtener el nickname del jugador rival (el jugador que no es el local)
        string nombreRival = "JUGADOR 2";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.IsLocal && !string.IsNullOrEmpty(p.NickName))
            {
                nombreRival = p.NickName.ToUpper();
                break;
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            btnIniciarPartida.gameObject.SetActive(true);

            if (totalJugadores >= 2)
            {
                txtEstadoJugadores.text = "¡" + nombreRival + " CONECTADO! Listo para iniciar.";
                btnIniciarPartida.interactable = true;
            }
            else
            {
                txtEstadoJugadores.text = "Esperando que se una una un jugador...";
                btnIniciarPartida.interactable = false;
            }
        }
        else
        {
            btnIniciarPartida.gameObject.SetActive(false);

            // Obtener el nickname del Anfitrión (Host)
            string nombreHost = "ANFITRIÓN";
            if (PhotonNetwork.MasterClient != null && !string.IsNullOrEmpty(PhotonNetwork.MasterClient.NickName))
            {
                nombreHost = PhotonNetwork.MasterClient.NickName.ToUpper();
            }

            txtEstadoJugadores.text = "¡Conectado con " + nombreHost + "! Esperando que el anfitrion inicie...";
        }
    }
}
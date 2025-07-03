using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Meta : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public void RetrunToLobby()
    {
        SceneManager.LoadScene(1);
    }

    private void Start()
    {
        Debug.Log($"클라우드 리전: {PhotonNetwork.CloudRegion}");
        Text.text = $"{PhotonNetwork.CloudRegion}";
    }

}

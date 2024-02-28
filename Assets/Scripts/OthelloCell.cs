// OthelloCell.cs
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class OthelloCell : MonoBehaviourPun
{
    int ownerID = -1;
    public UnityEngine.UI.Image ChipImage;
    public Vector2 Location;

    public int OwnerID
    {
        get { return ownerID; }
        set
        {
            ownerID = value;
            if (ChipImage != null && OthelloBoard.Instance != null && OthelloBoard.Instance.PlayerChipColors.Count > ownerID + 1)
            {
                ChipImage.color = OthelloBoard.Instance.PlayerChipColors[ownerID + 1];
            }
            if (ownerID == -1 && GetComponent<Button>() != null)
            {
                GetComponent<Button>().interactable = true;
            }
            else if (GetComponent<Button>() != null)
            {
                GetComponent<Button>().interactable = false;
            }
        }
    }

    public void CellPressed()
    {
        if (photonView != null && photonView.IsMine && OthelloBoard.Instance != null && OthelloBoard.Instance.CanPlaceHere(this.Location))
        {
            OthelloBoard.Instance.PlaceHere(this);
            OthelloBoard.Instance.EndTurn(false);
            photonView.RPC("CmdCellPressed", RpcTarget.All, this.Location);
        }
    }

    //[...]
    [PunRPC]
    public void CmdCellPressed(Vector2 location)
    {
        if (OthelloBoard.Instance != null)
        {
            UnityEngine.Debug.Log("CmdCellPressed RPC received. Location: " + location);
            OthelloBoard.Instance.ServerPlaceHere(location);
            photonView.RPC("RpcEndTurn", RpcTarget.All, false);
        }
    }
    //[...]

}
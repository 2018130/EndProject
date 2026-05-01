using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace ETFXPEL
{

public class UICanvasManager : MonoBehaviour {
	public static UICanvasManager GlobalAccess;
	void Awake () {
		GlobalAccess = this;
	}

	public bool MouseOverButton = false;
	public Text PENameText;
	public Text ToolTipText;

	// Use this for initialization
	void Start () {
		if (PENameText != null)
			PENameText.text = ParticleEffectsLibrary.GlobalAccess.GetCurrentPENameString();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void UpdateToolTip(ButtonTypes toolTipType) {
		if (ToolTipText != null) {
			if (toolTipType == ButtonTypes.Previous) {
				ToolTipText.text = "Select Previous Particle Effect";
			}
			else if (toolTipType == ButtonTypes.Next) {
				ToolTipText.text = "Select Next Particle Effect";
			}
		}
	}
	public void ClearToolTip() {
		if (ToolTipText != null) {
			ToolTipText.text = "";
		}
	}

	private void SelectPreviousPE() {
		// Previous
		ParticleEffectsLibrary.GlobalAccess.PreviousParticleEffect();
		if (PENameText != null)
			PENameText.text = ParticleEffectsLibrary.GlobalAccess.GetCurrentPENameString();
	}
	private void SelectNextPE() {
		// Next
		ParticleEffectsLibrary.GlobalAccess.NextParticleEffect();
		if (PENameText != null)
			PENameText.text = ParticleEffectsLibrary.GlobalAccess.GetCurrentPENameString();
	}

	private RaycastHit rayHit;

	/// <summary>
	/// User interfaces the button click.
	/// </summary>
	/// <param name="buttonTypeClicked">Button type clicked.</param>
	public void UIButtonClick(ButtonTypes buttonTypeClicked) {
		switch (buttonTypeClicked) {
		case ButtonTypes.Previous:
			// Select Previous Prefab
			SelectPreviousPE();
			break;
		case ButtonTypes.Next:
			// Select Next Prefab
			SelectNextPE();
			break;
		default:
			// Nothing
			break;
		}
	}
}
}
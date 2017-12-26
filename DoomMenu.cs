using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WadTools;

/*
This class builds the menu UI, and is controlled externally from GameSetup.cs

Currently hardwired to Doom 2's main menu, needs a lot of expansion.
*/

public class DoomMenu {

	private Canvas menuCanvas;
	private GameObject gameObject;
	private WadFile wad;
	private Material menuMaterial;
	
	private int currentItem = 0;
	private int itemCount = 5;

	private Image cursorImage;

	private Sprite cursorAnim1;
	private Sprite cursorAnim2;

	private float flashTimer;
	private float flashTimeMax = 0.228f;

	private GameObject mainMenu;

	public bool background = true;

	private AudioSource audioSource;

	public DoomMenu (WadFile inwad) {
		wad = inwad;
		gameObject = new GameObject("Menu");
		menuCanvas = gameObject.AddComponent<Canvas>();
		menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		menuCanvas.pixelPerfect = true;

		CanvasScaler menuCanvasScaler = gameObject.AddComponent<CanvasScaler>();
		menuCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		menuCanvasScaler.referenceResolution = new Vector2(320,200);

		gameObject.AddComponent<GraphicRaycaster>();

		menuMaterial = new Material(Shader.Find("Doom/Unlit Texture"));
		menuMaterial.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
		menuMaterial.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());

		mainMenu = new GameObject("Main Menu");
		mainMenu.transform.parent = gameObject.transform;
		mainMenu.transform.localPosition = new Vector3(-160f, 100f, 1f);

		BuildMenu(mainMenu);
		BuildCursor(mainMenu);

		mainMenu.active = false;

		InitSounds();
	}

	// Sounds
	private AudioClip soundActivate;
	private AudioClip soundBackup;
	private AudioClip soundPrompt;
	private AudioClip soundCursor;
	private AudioClip soundChange;
	private AudioClip soundInvalid;
	private AudioClip soundDismiss;
	private AudioClip soundChoose;
	private AudioClip soundClear;

	private void InitSounds() {
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.spatialBlend = 0.0f;

		soundActivate = new DoomSound(wad.GetLump("DSSWTCHN"), "Menu/Activate").ToAudioClip();
		soundBackup = new DoomSound(wad.GetLump("DSSWTCHN"), "Menu/Backup").ToAudioClip();
		soundPrompt = new DoomSound(wad.GetLump("DSSWTCHN"), "Menu/Prompt").ToAudioClip();
		soundCursor = new DoomSound(wad.GetLump("DSPSTOP"), "Menu/Cursor").ToAudioClip();
		soundChange = new DoomSound(wad.GetLump("DSSTNMOV"), "Menu/Change").ToAudioClip();
		soundInvalid = new DoomSound(wad.GetLump("DSOOF"), "Menu/Invalid").ToAudioClip();
		soundDismiss = new DoomSound(wad.GetLump("DSSWTCHX"), "Menu/Dismiss").ToAudioClip();
		soundChoose = new DoomSound(wad.GetLump("DSPISTOL"), "Menu/Choose").ToAudioClip();
		soundClear = new DoomSound(wad.GetLump("DSSWTCHX"), "Menu/Clear").ToAudioClip();
	}

	private void BuildMenu(GameObject parent) {
		AddPatch(94, 2, "M_DOOM", parent);
		AddPatch(97, 72 + 16, "M_NGAME", parent);
		AddPatch(97, 72 + 32, "M_OPTION", parent);
		AddPatch(97, 72 + 48, "M_LOADG", parent);
		AddPatch(97, 72 + 64, "M_SAVEG", parent);
		AddPatch(97, 72 + 80, "M_QUITG", parent);
	}

	private void BuildCursor(GameObject parent) {
		cursorAnim1 = DoomGraphic.BuildSprite("M_SKULL1", GameSetup.wad);
		cursorAnim2 = DoomGraphic.BuildSprite("M_SKULL2", GameSetup.wad);
		cursorImage = AddPatch(65, 83, "M_SKULL1", parent);
	}

	private Image AddPatch(int x, int y, string patchName, GameObject parent = null) {
		Sprite logo = DoomGraphic.BuildSprite(patchName, GameSetup.wad);
		GameObject newPatch = new GameObject(patchName);
		Image img = newPatch.AddComponent<Image>();
		img.material = menuMaterial;
		img.sprite = logo;
		if (parent != null) {
			newPatch.transform.SetParent(parent.transform, false);
		} else {
			newPatch.transform.SetParent(gameObject.transform, false);
		}
		img.SetNativeSize();
		img.rectTransform.anchorMin = new Vector2(0f, 1f);
		img.rectTransform.anchorMax = new Vector2(0f, 1f);
		img.rectTransform.anchoredPosition = new Vector2(x, -y);
		img.rectTransform.pivot = new Vector2(logo.pivot.x / Mathf.Pow(img.rectTransform.sizeDelta.x, 2),
											  1.0f - (logo.pivot.y / Mathf.Pow(img.rectTransform.sizeDelta.y, 2)));

		return img;
	}

	public void Show(bool show, bool silent = false) {
		if (!silent) audioSource.PlayOneShot(show?soundActivate:soundDismiss);
		mainMenu.SetActive(show);
	}

	public int Accept() {
		audioSource.PlayOneShot(soundChoose);
		return currentItem;
	}

	public void Toggle() {
		mainMenu.active = !mainMenu.active;
	}

	public void Up() {
		audioSource.PlayOneShot(soundCursor);
		currentItem -= 1;
		if (currentItem < 0) currentItem = itemCount - 1;
		cursorImage.rectTransform.anchoredPosition = new Vector2(64,  - (83 +(currentItem * 16)) );
	}

	public void Down() {
		audioSource.PlayOneShot(soundCursor);
		currentItem += 1;
		if (currentItem == itemCount) currentItem = 0;
		cursorImage.rectTransform.anchoredPosition = new Vector2(64,  - (83 +(currentItem * 16)) );
	}

	public void Update(float timer) {
		flashTimer += timer;
		if (flashTimer > flashTimeMax) {
			flashTimer -= flashTimeMax;
			FlashCursor();
		}
	}

	private void FlashCursor() {
		if (cursorImage.sprite == cursorAnim1) {
			cursorImage.sprite = cursorAnim2;
		} else {
			cursorImage.sprite = cursorAnim1;
		}
	}
}

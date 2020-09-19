﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using BepInEx;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;


namespace BetterUI
{

    class DPSMeter
    {
        private readonly BetterUI mod;
        private readonly Queue<DamageLog> characterDamageLog = new Queue<DamageLog>();
        private float characterDamageSum = 0;
        private readonly Queue<DamageLog> minionDamageLog = new Queue<DamageLog>();
        private float minionDamageSum = 0;

        private HGTextMeshProUGUI textMesh;
        public float DPS { get => MinionDPS + CharacterDPS; }
        public float CharacterDPS { get => characterDamageLog.Count > 0 ? characterDamageSum / Clamp(Time.time - characterDamageLog.Peek().time) : 0; }
        public float MinionDPS { get => minionDamageLog.Count > 0 ? minionDamageSum / Clamp(Time.time - minionDamageLog.Peek().time) : 0; }
        internal struct DamageLog
        {
            public float damage;
            public float time;
            public DamageLog(float dmg)
            {
                damage = dmg;
                time = Time.time;
            }
        }
        public DPSMeter(BetterUI m)
        {
            mod = m;
        }
        public float Clamp(float value)
        {
            return Math.Min(Math.Max(1, value), mod.config.DPSMeterTimespan.Value);
        }

        public void Update()
        {
            
            while(characterDamageLog.Count > 0 && characterDamageLog.Peek().time < Time.time - mod.config.DPSMeterTimespan.Value)
            {
                characterDamageSum -= characterDamageLog.Dequeue().damage;
            }
            while (minionDamageLog.Count > 0 && minionDamageLog.Peek().time < Time.time - mod.config.DPSMeterTimespan.Value)
            {
                minionDamageSum -= minionDamageLog.Dequeue().damage;
            }
            if (textMesh != null)
            {
                textMesh.text = "DPS: " + (mod.config.DPSMeterWindowIncludeMinions.Value ? DPS : CharacterDPS).ToString("N0") ;
            }
        }

        public void hook_ClientDamageNotified(On.RoR2.GlobalEventManager.orig_ClientDamageNotified orig, DamageDealtMessage dmgMsg)
        {
            orig(dmgMsg);

            CharacterMaster localMaster = LocalUserManager.GetFirstLocalUser().cachedMasterController.master;



            if (dmgMsg != null && dmgMsg.attacker != null)
            {
                if (dmgMsg.attacker == localMaster.GetBodyObject())
                {
                    characterDamageSum += dmgMsg.damage;
                    characterDamageLog.Enqueue(new DamageLog(dmgMsg.damage));
                }
                else if (dmgMsg.attacker.GetComponent<CharacterBody>() != null &&
                    dmgMsg.attacker.GetComponent<CharacterBody>().master != null &&
                    dmgMsg.attacker.GetComponent<CharacterBody>().master.minionOwnership != null &&
                    dmgMsg.attacker.GetComponent<CharacterBody>().master.minionOwnership.ownerMasterId == localMaster.netId)
                {
                    minionDamageSum += dmgMsg.damage;
                    minionDamageLog.Enqueue(new DamageLog(dmgMsg.damage));
                }
            }
        }

        public void hook_Awake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            orig(self);

            GameObject DPSMeterPanel = new GameObject("DPSMeterPanel");
            RectTransform rectTransform = DPSMeterPanel.AddComponent<RectTransform>();

            DPSMeterPanel.transform.SetParent(self.mainContainer.transform);
            DPSMeterPanel.transform.SetAsFirstSibling();



            GameObject DPSMeterText = new GameObject("DPSMeterText");
            RectTransform rectTransform2 = DPSMeterText.AddComponent<RectTransform>();
            textMesh = DPSMeterText.AddComponent<HGTextMeshProUGUI>();

            DPSMeterText.transform.SetParent(DPSMeterPanel.transform);


            rectTransform.localPosition = new Vector3(0, 0, 0);
            rectTransform.anchorMin = mod.config.DPSMeterWindowAnchorMin.Value;
            rectTransform.anchorMax = mod.config.DPSMeterWindowAnchorMax.Value;
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = mod.config.DPSMeterWindowPivot.Value;
            rectTransform.sizeDelta = mod.config.DPSMeterWindowSize.Value;
            rectTransform.anchoredPosition = mod.config.DPSMeterWindowPosition.Value;
            rectTransform.eulerAngles = mod.config.DPSMeterWindowAngle.Value;
             
            rectTransform2.localPosition = Vector3.zero;
            rectTransform2.anchorMin = Vector2.zero;
            rectTransform2.anchorMax = Vector2.one;
            rectTransform2.localScale = Vector3.one;
            rectTransform2.sizeDelta = new Vector2(-12, -12);
            rectTransform2.anchoredPosition = Vector2.zero;

            if (mod.config.DPSMeterWindowBackground.Value)
            {
                Image image = DPSMeterPanel.AddComponent<Image>();
                Image copyImage = self.itemInventoryDisplay.gameObject.GetComponent<Image>();
                image.sprite = copyImage.sprite;
                image.color = copyImage.color;
                image.type = Image.Type.Sliced;
            }

            textMesh.enableAutoSizing = true;
            textMesh.fontSizeMax = 256;
            textMesh.faceColor = Color.white;
            textMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        }
    }
}

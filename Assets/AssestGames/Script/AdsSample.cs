//using System;
//using Unity.Services.LevelPlay;
//using UnityEngine;

//public class AdsSample : MonoBehaviour
//{
//    // Unity Script | 0 references
//    private LevelPlayBannerAd bannerAd;
//    private LevelPlayInterstitialAd interstitialAd;
//    private LevelPlayRewardedAd rewardedVideoAd;
//    bool isAdsEnabled = false;

//    // Start is called once before the first execution of Update after the MonoBehaviour is
//    // Unity Message | 0 references
//    void Start()
//    {
//        LevelPlay.ValidateIntegration();

//        LevelPlay.OnInitSuccess += SdkInitCompletedEvent;
//        LevelPlay.OnInitFailed += SdkInitFailedEvent;

//        LevelPlay.Init(AdConfig.AppKey);
//    }

//    // 1 reference
//    private void SdkInitFailedEvent(LevelPlayInitError error)
//    {
//        Debug.Log($"Ads nor enable : {error}");
//    }

//    // 1 reference
//    private void SdkInitCompletedEvent(LevelPlayConfiguration configuration)
//    {
//        EnableAds();
//        isAdsEnabled = true;
//    }

//    // 1 reference
//    void EnableAds()
//    {
//        var configBuilder = new LevelPlayBannerAd.Config.Builder();
//        configBuilder.SetSize(LevelPlayAdSize.BANNER)
//            .SetPosition(LevelPlayBannerPosition.BottomRight);
//        var bannerConfig = configBuilder.Build();

//        bannerAd = new LevelPlayBannerAd(AdConfig.BannerAdUnitId, bannerConfig);

//        interstitialAd = new LevelPlayInterstitialAd(AdConfig.InterstitialAdUnitId);

//        rewardedVideoAd = new LevelPlayRewardedAd(AdConfig.RewardedVideoAdUnitId);
//    }

//    // 0 references
//    public void LoadBanderAds()
//    {
//        bannerAd.LoadAd();
//    }

//    // 0 references
//    public void ShowBanderAds()
//    {
//        bannerAd.ShowAd();
//    }

//    // 0 references
//    public void HideBanderAds()
//    {
//        bannerAd.HideAd();
//    }

//    // 0 references
//    public void LoadInterstitialAds()
//    {
//        interstitialAd.LoadAd();
//    }

//// 0 references
//    public void ShowInterstitialAds()
//    {
//        if (interstitialAd.IsAdReady())
//        {
//            interstitialAd.ShowAd();
//        }
//        else
//        {
//            Debug.Log("[LevelPlaySample] LevelPlay Interstitial Ad is not ready");
//        }
//    }

//// 0 references
//    public void LoadRewardedVideoAds()
//    {
//        rewardedVideoAd.LoadAd();
//    }

//    // 0 references
//    public void ShowRewardedVideoAds()
//    {
//        if (rewardedVideoAd.IsAdReady())
//        {
//            rewardedVideoAd.ShowAd();
//        }
//        else
//        {
//            Debug.Log("[LevelPlaySample] LevelPlay Rewarded Video Ad is not ready");
//        }
//    }

//}
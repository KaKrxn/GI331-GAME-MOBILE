using UnityEngine;

public static class AdConfig
{
    // 4 references
    public static string AppKey => GetAppKey();
    public static string BannerAdUnitId => GetBannerAdUnitId();
    public static string InterstitialAdUnitId => GetInterstitialAdUnitId();
    public static string RewardedVideoAdUnitId => GetRewardedVideoAdUnitId();

    // 1 reference
    static string GetAppKey()
    {
#if UNITY_ANDROID
        return "5966959";
#elif UNITY_IPHONE
        return "5966958";
#else
        return "unexpected_platform";
#endif
    }

    // 1 reference
    static string GetBannerAdUnitId()
    {
#if UNITY_ANDROID
        return "k2nn9fdkpoltn6ap";
#elif UNITY_IPHONE
        return "hzi8d1i24k4gvfck";
#else
        return "unexpected_platform";
#endif
    }

    // 1 reference
    static string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return "wn8gqj6smd59ugg7";
#elif UNITY_IPHONE
        return "0j07ay1hplbbnu6h";
#else
        return "unexpected_platform";
#endif
    }

    // 1 reference
    static string GetRewardedVideoAdUnitId()
    {
#if UNITY_ANDROID
        return "771brpf58gjeoqhk";
#elif UNITY_IPHONE
        return "a7rnsev41kokoh95";
#else
        return "unexpected_platform";
#endif
    }
}
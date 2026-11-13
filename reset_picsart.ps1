param(
    [string]$Device = "",
    [ValidateSet("keep", "full", "monthly", "plus", "ultra", "deeplink", "")]
    [string]$Mode = ""
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Get-RandomHex([int]$Length) {
    $chars = "0123456789abcdef"
    $res = ""
    for ($i = 0; $i -lt $Length; $i++) {
        $res += $chars[(Get-Random -Minimum 0 -Maximum 16)]
    }
    return $res
}

function Get-ConnectedDevices {
    $lines = & adb devices 2>$null
    $devices = @()
    foreach ($line in $lines) {
        if ($line -match "^(\S+)\s+device$") {
            $devices += $Matches[1]
        }
    }
    return $devices
}

function Reset-PicsartOnDevice([string]$Serial, [string]$ActionMode) {
    Write-Host "`n====================================================" -ForegroundColor Cyan
    Write-Host "[*] Dang xu ly thiet bi: $Serial (Che do: $ActionMode)" -ForegroundColor Cyan
    Write-Host "====================================================" -ForegroundColor Cyan

    & adb -s $Serial root | Out-Null
    $newDeviceId = Get-RandomHex 32
    $fixPerm = "PUID=`$(stat -c '%u' /data/data/com.picsart.studio); chown -R `$PUID:`$PUID /data/data/com.picsart.studio/shared_prefs /data/data/com.picsart.studio/files /data/data/com.picsart.studio/cache 2>/dev/null; restorecon -R /data/data/com.picsart.studio 2>/dev/null; "
    $cleanSub = "sed -i 's/d_[0-9a-f]\{32\}/d_$newDeviceId/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml /data/data/com.picsart.studio/shared_prefs/participated_experiments_prefs_key.xml /data/data/com.picsart.studio/cache/bugsnag/user-info 2>/dev/null; sed -i 's/\`"subscription_status\`":\`"subscribed\`"/\`"subscription_status\`":\`"not_subscribed\`"/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.tag_settings_cache.xml 2>/dev/null; sed -i 's/app_start_subscription_status\`" value=\`"true/app_start_subscription_status\`" value=\`"false/g; s/SUBSCRIPTION\`" value=\`"true/SUBSCRIPTION\`" value=\`"false/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.studio_preferences.xml 2>/dev/null; rm -f /data/data/com.picsart.studio/files/device-id /data/data/com.picsart.studio/files/internal-device-id /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml /data/data/com.picsart.studio/shared_prefs/com.facebook.internal.iap.IAP_CACHE_GPBLV2V7.xml; "
    $patchMonthly = "sed -i 's/\`"pro_weekly_100\`",\`"is_selected\`"/\`"pro_monthly\`",\`"is_selected\`"/g; s/\`"pro_weekly\`",\`"is_selected\`"/\`"pro_monthly\`",\`"is_selected\`"/g; s/\`"ultra_monthly\`",\`"is_selected\`"/\`"pro_monthly\`",\`"is_selected\`"/g; s/\`"ultra_yearly\`",\`"is_selected\`"/\`"pro_yearly_trial\`",\`"is_selected\`"/g; s/\`"plus_weekly\`",\`"is_selected\`"/\`"plus_monthly\`",\`"is_selected\`"/g; s/\`"package_id\`":\`"subscription_pro_weekly\`"/\`"package_id\`":\`"subscription_pro_monthly\`"/g; s/\`"package_id\`":\`"subscription_ultra_monthly\`"/\`"package_id\`":\`"subscription_pro_monthly\`"/g; s/\`"package_id\`":\`"subscription_plus_weekly\`"/\`"package_id\`":\`"subscription_plus_tier_monthly\`"/g; s/title_12/title_3/g; s/subtitle_8/subtitle_2/g; s/\`"package_sku\`":\`"subscription_pro_monthly_100\`"/\`"package_sku\`":\`"subscription_pro_monthly\`"/g; s/\`"package_sku\`":\`"subscription_pro_1_monthly\`"/\`"package_sku\`":\`"subscription_pro_monthly\`"/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; "
    $patchUltra = "sed -i 's/\`"package_sku\`":\`"subscription_ultra_monthly_750_credits\`"/\`"package_sku\`":\`"subscription_ultra_monthly\`"/g; s/\`"package_sku\`":\`"subscription_ultra_yearly_750_credits\`"/\`"package_sku\`":\`"subscription_ultra_yearly\`"/g; s/\`"pro_weekly_100\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"pro_weekly_100\`",\`"is_selected\`"/\`"ultra_monthly\`",\`"is_selected\`"/g; s/\`"pro_weekly\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"pro_weekly\`",\`"is_selected\`"/\`"ultra_monthly\`",\`"is_selected\`"/g; s/\`"pro_monthly\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"pro_monthly\`",\`"is_selected\`"/\`"ultra_monthly\`",\`"is_selected\`"/g; s/\`"plus_weekly\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"plus_weekly\`",\`"is_selected\`"/\`"ultra_monthly\`",\`"is_selected\`"/g; s/\`"plus_monthly\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"plus_monthly\`",\`"is_selected\`"/\`"ultra_monthly\`",\`"is_selected\`"/g; s/\`"ultra_monthly\`",\`"is_selected\`":false/\`"ultra_monthly\`",\`"is_selected\`":true/g; s/\`"pro_yearly_trial\`",\`"is_selected\`":true/\`"ultra_yearly\`",\`"is_selected\`":false/g; s/\`"pro_yearly_trial\`",\`"is_selected\`"/\`"ultra_yearly\`",\`"is_selected\`"/g; s/\`"pro_yearly_wo_trial\`",\`"is_selected\`":true/\`"ultra_yearly\`",\`"is_selected\`":false/g; s/\`"pro_yearly_wo_trial\`",\`"is_selected\`"/\`"ultra_yearly\`",\`"is_selected\`"/g; s/\`"plus_yearly_trial\`",\`"is_selected\`":true/\`"ultra_yearly\`",\`"is_selected\`":false/g; s/\`"plus_yearly_trial\`",\`"is_selected\`"/\`"ultra_yearly\`",\`"is_selected\`"/g; s/\`"plus_yearly_wo_trial\`",\`"is_selected\`":true/\`"ultra_yearly\`",\`"is_selected\`":false/g; s/\`"plus_yearly_wo_trial\`",\`"is_selected\`"/\`"ultra_yearly\`",\`"is_selected\`"/g; s/\`"ultra_yearly\`",\`"is_selected\`":true/\`"ultra_yearly\`",\`"is_selected\`":false/g; s/\`"package_id\`":\`"subscription_pro_weekly\`"/\`"package_id\`":\`"subscription_ultra_monthly\`"/g; s/\`"package_id\`":\`"subscription_pro_monthly\`"/\`"package_id\`":\`"subscription_ultra_monthly\`"/g; s/\`"package_id\`":\`"subscription_plus_weekly\`"/\`"package_id\`":\`"subscription_ultra_monthly\`"/g; s/\`"package_id\`":\`"subscription_plus_tier_monthly\`"/\`"package_id\`":\`"subscription_ultra_monthly\`"/g; s/\`"content_id\`":\`"plus_tab_default\`"}/\`"content_id\`":\`"pro_tab_default\`"}/g; s/\`"content_id\`":\`"pro_tab_ultra\`",\`"selected\`"/\`"content_id\`":\`"pro_tab_default\`",\`"selected\`"/g; s/\`"content_id\`":\`"ultra_yearly\`",\`"badge\`"/\`"content_id\`":\`"pro_tab_default\`",\`"badge\`"/g; s/\`"content_id\`":\`"ultra_yearly_upgrade\`",\`"selected\`"/\`"content_id\`":\`"pro_tab_default\`",\`"selected\`"/g; s/title_3/title_12/g; s/subtitle_2/subtitle_8/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; "

    if ($ActionMode -eq "deeplink") {
        Write-Host " -> Mo thang Bang gia (DeepLink)..." -ForegroundColor Yellow
        $cmd = "am start -a android.intent.action.VIEW -d 'picsart://subscription?type=full-screen-promotion&subscription_force_enabled=true&source=profile' com.picsart.studio"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Da mo Bang gia tren may $Serial!" -ForegroundColor Green
    }
    elseif ($ActionMode -eq "ultra") {
        Write-Host " -> Ep hien Goi Ultra (1 Thang - 1500 Credit)..." -ForegroundColor Yellow
        $cmd = "am force-stop com.picsart.studio; " +
               $cleanSub +
               $patchUltra +
               $fixPerm +
               "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Da chuyen sang Goi Ultra (1 Thang - 1500 Credit) tren may $Serial!" -ForegroundColor Green
    }
    elseif ($ActionMode -eq "plus") {
        Write-Host " -> Ep hien Goi Plus (1 Thang)..." -ForegroundColor Magenta
        $cmd = "am force-stop com.picsart.studio; " +
               $cleanSub +
               $patchMonthly +
               "sed -i 's/\`"content_id\`":\`"pro_tab_default\`"}/\`"content_id\`":\`"plus_tab_default\`"}/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; " +
               $fixPerm +
               "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Da chuyen sang Goi Plus (1 Thang) tren may $Serial!" -ForegroundColor Green
    }
    elseif ($ActionMode -eq "full") {
        Write-Host " -> Xoa data Picsart va cap san device_id moi d_$newDeviceId (Giu nguyen Gmail & Bang gia)..." -ForegroundColor Gray
        $cmd = "cp /data/data/com.picsart.studio/files/settings.json /sdcard/.picsart_settings_bak.json 2>/dev/null; " +
               "pm clear com.picsart.studio; mkdir -p /data/data/com.picsart.studio/shared_prefs /data/data/com.picsart.studio/files; " +
               "if [ -f /sdcard/.picsart_settings_bak.json ]; then cp /sdcard/.picsart_settings_bak.json /data/data/com.picsart.studio/files/settings.json; rm -f /sdcard/.picsart_settings_bak.json; fi; " +
               "printf '<?xml version=\`"1.0\`" encoding=\`"utf-8\`" standalone=\`"yes\`" ?>\n<map>\n    <string name=\`"device_id\`">d_$newDeviceId</string>\n</map>\n' > /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml; " +
               "chmod 660 /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml; " +
               $fixPerm +
               "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Hoan tat Reset Picsart tren may $Serial (Gmail an toan 100%)!" -ForegroundColor Green
    }
    elseif ($ActionMode -eq "monthly") {
        Write-Host " -> Ep cau hinh Goi Pro 1 Thang (pro_monthly)..." -ForegroundColor Gray
        $cmd = "am force-stop com.picsart.studio; " +
               $cleanSub +
               $patchMonthly +
               "sed -i 's/\`"content_id\`":\`"plus_tab_default\`"}/\`"content_id\`":\`"pro_tab_default\`"}/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; " +
               $fixPerm +
               "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Da ep Goi Pro 1 Thang tren may $Serial!" -ForegroundColor Green
    }
    else {
        Write-Host " -> Doi Picsart device_id -> d_$newDeviceId, xoa goi Pro cu & ep 1 Thang (Giu nguyen Gmail & Nick)..." -ForegroundColor Gray
        $cmd = "am force-stop com.picsart.studio; " +
               $cleanSub +
               $patchMonthly +
               $fixPerm +
               "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1"
        & adb -s $Serial shell $cmd 2>$null | Out-Null
        Write-Host "[OK] Hoan tat Fix Pro + Ep 1 Thang tren may $Serial (Gmail an toan 100%)!" -ForegroundColor Green
    }
}

$allDevices = Get-ConnectedDevices
if ($Device -ne "") {
    $runMode = if ($Mode -ne "") { $Mode } else { "keep" }
    if ($Device -eq "all") {
        foreach ($d in $allDevices) { Reset-PicsartOnDevice -Serial $d -ActionMode $runMode }
    }
    else {
        Reset-PicsartOnDevice -Serial $Device -ActionMode $runMode
    }
}

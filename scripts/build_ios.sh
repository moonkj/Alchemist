#!/bin/bash
# 컬러 믹스: 연금술사 — iOS 무선 설치 파이프라인
# 사용: ./scripts/build_ios.sh
# 전제: Unity 6000.0.32f1 + iOS Build Support 설치, iPhone 무선 페어링 완료
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_APP="/Applications/Unity/Hub/Editor/6000.0.32f1/Unity.app/Contents/MacOS/Unity"
XCODE_OUTPUT="$PROJECT_DIR/Builds/iOS"
ARCHIVE_PATH="$PROJECT_DIR/Builds/Alchemist.xcarchive"
EXPORT_PATH="$PROJECT_DIR/Builds/Export"
DEVICE_UDID="${DEVICE_UDID:-835A5E84-05B4-520C-B52C-E69BBEE38FED}"
TEAM_ID="${TEAM_ID:-QN975MTM7H}"
BUNDLE_ID="com.moonkj.colormixalchemist"

echo "==> [1/4] Unity batch build (iOS)"
"$UNITY_APP" \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$PROJECT_DIR" \
  -executeMethod Alchemist.EditorTools.BuildScript.BuildIOS \
  -logFile "$PROJECT_DIR/Builds/unity-build.log"

echo "==> [2/4] Xcode archive (Release)"
xcodebuild \
  -project "$XCODE_OUTPUT/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone \
  -configuration Release \
  -destination 'generic/platform=iOS' \
  -archivePath "$ARCHIVE_PATH" \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$TEAM_ID" \
  CODE_SIGN_STYLE=Automatic \
  PRODUCT_BUNDLE_IDENTIFIER="$BUNDLE_ID" \
  archive | xcpretty || true

echo "==> [3/4] Export .ipa for development"
cat > /tmp/ExportOptions-dev.plist <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key><string>development</string>
  <key>teamID</key><string>$TEAM_ID</string>
  <key>signingStyle</key><string>automatic</string>
  <key>destination</key><string>export</string>
  <key>stripSwiftSymbols</key><true/>
  <key>thinning</key><string>&lt;none&gt;</string>
</dict>
</plist>
EOF

xcodebuild \
  -exportArchive \
  -archivePath "$ARCHIVE_PATH" \
  -exportPath "$EXPORT_PATH" \
  -exportOptionsPlist /tmp/ExportOptions-dev.plist \
  -allowProvisioningUpdates

echo "==> [4/4] Install on wireless device ($DEVICE_UDID)"
APP_BUNDLE="$EXPORT_PATH/ColorMixAlchemist.app"
if [ ! -d "$APP_BUNDLE" ]; then
  APP_BUNDLE="$(find "$EXPORT_PATH" -maxdepth 2 -name '*.app' | head -1)"
fi
xcrun devicectl device install app --device "$DEVICE_UDID" "$APP_BUNDLE"

echo "✅ 설치 완료. iPhone 홈 화면에서 앱 실행."

"""Screenshot da Game view (inclui HUD overlay) com seed 12345 as 12h. Tamanho = o da Game view do Editor."""
import os, sys, time, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ub, run_baseline as rb
out = rb.fwd(os.path.join(rb.BASE_DIR, "gameview_hud_s12345_h12.png"))
rb.setup(12345)
ub.run_code('var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>(); dn.autoAdvanceTime=false; dn.timeOfDay=12f; UnityEngine.Camera.main.ResetAspect(); return true;')
time.sleep(2)
print(ub.run_code('UnityEngine.ScreenCapture.CaptureScreenshot("%s"); return UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " cam.aspect=" + UnityEngine.Camera.main.aspect;' % out))
time.sleep(2)
print(os.path.exists(out), os.path.getsize(out) if os.path.exists(out) else 0)

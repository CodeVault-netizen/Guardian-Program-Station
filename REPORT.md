# تقرير الحالة — Guardian Program Station

## 🆕 CLI متعدد المنصات (Guardian.ProgramStation.Cli)

### البنية المعمارية
```
CLI (guardian-program-station)
  ↓ (ProjectReference → Kernel فقط — لا UI، لا Avalonia)
Kernel (Composition Root — نفس الذي يستخدمه GUI)
  ↓
Application Use Cases
  ↓
Core / Infrastructure
```

| الأمر | الـ Use Case المستخدم | الملف |
|---|---|---|
| `create` | `CreateTreeUseCase` (نفس زر "Create on Disk" في GUI) | `Cli/Commands/CreateCommand.cs` |
| `preview` | `PreviewTreeUseCase` (جديد، مشارك مع GUI) | `Cli/Commands/PreviewCommand.cs` |
| `validate` | `ValidateTreeUseCase` (جديد، مشارك مع GUI) | `Cli/Commands/ValidateCommand.cs` |
| `template list` | `ITreeService.LoadTreesAsync` | `Cli/Commands/TemplateCommand.cs` |
| `template create` | `ITreeService.LoadTreeFromFileAsync` + `SaveTreeAsync` | `Cli/Commands/TemplateCommand.cs` |
| `template import` | `ImportTreeUseCase` (نفس استيراد GUI) | `Cli/Commands/TemplateCommand.cs` |
| `template export` | `ITreeService.LoadTreeAsync` + `SaveTreeToFileAsync` | `Cli/Commands/TemplateCommand.cs` |
| `template delete` | `ITreeService.DeleteTreeAsync` | `Cli/Commands/TemplateCommand.cs` |

### الملفات الجديدة
| الملف | الغرض |
|---|---|
| `Cli/Cli.csproj` | مشروع CLI مستقل (net10.0, System.CommandLine) |
| `Cli/Program.cs` | نقطة الدخول: UTF-8 + نفس Kernel Composition Root |
| `Cli/CliApplication.cs` | بناء شجرة الأوامر + تعيين Exit Codes |
| `Cli/ExitCodes.cs` | 0 نجاح / 1 خطأ عام / 2 وسائط غير صالحة / 3 خطأ تحقق / 4 فشل عملية |
| `Cli/Commands/CreateCommand.cs` | أمر create |
| `Cli/Commands/PreviewCommand.cs` | أمر preview |
| `Cli/Commands/ValidateCommand.cs` | أمر validate |
| `Cli/Commands/TemplateCommand.cs` | أمر template + 5 subcommands |
| `Application/use_cases/preview_tree_use_case.cs` | عرض الشجرة ASCII (مشارك GUI/CLI) |
| `Application/use_cases/validate_tree_use_case.cs` | التحقق من قواعد الشجرة (مشارك GUI/CLI) |
| `Directory.Build.props` | الإصدار الموحد `1.0.0` لكل المشاريع |
| `Tests/.../CliTests.cs` | 20 اختبار CLI |
| `Tests/.../TreeUseCaseTests.cs` | 10 اختبارات للـ use cases الجديدة |

### الملفات المعدّلة
| الملف | السبب |
|---|---|
| `GuardianProgramStation.sln` | إضافة مشروع Cli |
| `Kernel/service_collection_extensions.cs` | تسجيل PreviewTreeUseCase + ValidateTreeUseCase |
| `Tests/.../Guardian.ProgramStation.Tests.csproj` | إضافة مرجع Cli |

### حالة الأوامر (مختبرة فعلياً على Windows)
| الأمر | الحالة |
|---|---|
| `guardian-program-station --help` | ✅ يعمل (RC 0) |
| `guardian-program-station --version` | ✅ يعمل — يعرض `1.0.0` (RC 0) |
| `guardian-program-station` (بدون أمر) | ✅ يعرض help |
| `guardian-program-station create --tree X --path Y` | ✅ ينشئ المجلدات فعلياً (RC 0) |
| `guardian-program-station preview --tree X` | ✅ يعرض ASCII (├── └── │) |
| `guardian-program-station validate --tree X` | ✅ `Valid` / `Invalid` + الأخطاء |
| `guardian-program-station template list/create/import/export/delete` | ✅ كلها تعمل (RC 0) |
| أمر غير معروف / option خاطئ / وسيط ناقص | ✅ RC 2 |
| شجرة غير صالحة | ✅ RC 3 |
| ملف غير موجود في create | ✅ RC 4 |

### الـ Exit Codes (مختبرة)
```
0 = Success        (preview, validate صحيح, create, template)
1 = General error  (معرّف في العقد)
2 = Invalid arguments (أمر غير معروف، --bogus، وسيط مطلوب ناقص)
3 = Validation error  (validate على شجرة غير صالحة أو ملف مفقود)
4 = Operation failed  (create بملف شجرة غير موجود)
```

### Cross-platform
- `Cli.csproj` لا يعتمد على أي API خاص بـ Windows — يستخدم `Path.Combine`، `Path.GetFullPath`، `Environment.GetFolderPath` (عبر `PathHelper` في Infrastructure).
- لا يستخدم Registry ولا CMD/PowerShell/Bash — ناتج Exe صافٍ يعمل على أي نظام يشغّل .NET 10.
- الاختبار `Cli_DoesNotReferenceAvalonia` يثبت أن assembly الـ CLI لا يستطيع الوصول إلى Avalonia أو `Guardian.ProgramStation.UI` عبر سلسلة المراجع بأكملها.

### الاختبارات
```
88/88 PASS  (قبل: 51/51 → أُضيف 20 اختبار CLI + 10 use case + 7 من أعمال سابقة)
0 Errors / 0 Warnings
```

### المنصات
```
Windows: Tested ✅ (كل الأوامر + Exit Codes + UTF-8 preview)
Linux:   Architecturally ready / Not runtime-tested (لا API خاص بـ Windows)
macOS:   Architecturally ready / Not runtime-tested (لا API خاص بـ Windows)
```

### كيف تشغّل CLI
```bash
cd "D:\Visual Studio\Projects\Guardian Program Station"
dotnet run --project Cli -- --help
# أو مباشرة:
Cli/bin/Debug/net10.0/guardian-program-station --help
```

---

## 🚀 GitHub Actions — Cross-Platform CI (2026-08-13)

Workflow: **Cross-Platform CI** (`.github/workflows/ci.yml`) — matrix على
`windows-latest` / `ubuntu-latest` / `macos-latest`، مع `fail-fast: false`
لكي يُبلّغ كل نظام مستقلاً.

Run: https://github.com/CodeVault-netizen/Guardian-Program-Station/actions/runs/31704171467 — **SUCCESS** ✅

كل نظام نفّذ فعلياً: Restore → Build Release كامل الحل → كل الاختبارات →
`ci/verify-cli.sh` الذي يشغّل **الـ CLI الحقيقي** (37 فحصاً: help/version،
create مع فحص filesystem حقيقي، preview مع أسماء العقد وUTF-8، validate
Valid/Invalid، template create/list/export/import/delete، Exit Codes
0/2/3/4، وأسماء عربية/صينية/يابانية دون أحرف استبدال).

| المنصة | Build | Tests | CLI (37 فحصاً) | create | preview | validate | template | UTF-8 | filesystem |
|---|---|---|---|---|---|---|---|---|---|
| **Windows** | ✅ PASS | ✅ PASS (TRX مرفوع) | ✅ PASS | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Ubuntu Linux** | ✅ PASS | ✅ PASS (TRX مرفوع) | ✅ PASS | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **macOS** | ✅ PASS | ✅ PASS (TRX مرفوع) | ✅ PASS | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

> ملاحظة: سحب السجلات/المصنوعات (logs/artifacts) يتطلب مصادقة GitHub؛
> النتائج أعلاه من GitHub API الرسمي (حالة كل خطوة success). راجع
> صفحة الـ Run أعلاه لعرض السجلات الكاملة من حسابك.

**تحديث توافق المنصات (بعد الاختبار الفعلي):**
```
Windows: Tested ✅ (Build/Tests/CLI كلها PASS على runner حقيقي)
Linux:   Tested ✅ (Build/Tests/CLI كلها PASS على ubuntu-latest)
macOS:   Tested ✅ (Build/Tests/CLI كلها PASS على macos-latest)
```

الملفات المضافة لهذه المرحلة (CI فقط — لم يُعدَّل أي كود):
- `.github/workflows/ci.yml`
- `ci/verify-cli.sh`
- `ci/test-trees/{valid,invalid,utf8,file-node}.json`
- `.gitattributes` (LF إلزامي للسكربتات وبيانات الاختبار)

Commit: `cc5c0a6` — "Add cross-platform CI workflow that verifies the CLI on Windows, Linux and macOS"

### Artifacts CLI القابلة للتنزيل (Run #31705677567 — SUCCESS ✅)
كل Artifact مبني من نفس الـ commit الذي نجحت عليه الاختبارات، ويتم تشغيل
`--version` و `--help` عليه على الرنر قبل الرفع (`--self-contained false` =
يتطلب .NET runtime وليس SDK؛ قرار التعبئة النهائي مؤجل):

| Artifact | RID | الحجم | مُتحقق على الرنر (`--version`/`--help`) |
|---|---|---|---|
| `guardian-program-station-win-x64` | win-x64 | 454 KB | ✅ PASS |
| `guardian-program-station-linux-x64` | linux-x64 | 414 KB | ✅ PASS |
| `guardian-program-station-osx-arm64` | osx-arm64 | 412 KB | ✅ PASS |

> اكتشاف حقيقي: رنر `macos-latest` هو ARM64 (Apple Silicon)، لذا `osx-x64` لا
> يمكن تشغيله عليه (لا يوجد x64 runtime) — الـ RID يُحسب الآن من
> `RUNNER_OS`+`RUNNER_ARCH` في الـ Workflow (`win-x64` / `linux-x64` /
> `osx-arm64`) ليُبنى ويُتحقق من كل Artifact على الرنر الخاص به.

NuGet: كل الحزم السبعة مثبتة بإصدارات exact (System.CommandLine
`2.0.0-beta5.25277.114`، DI `10.0.0`، Test.Sdk `17.11.1`، xunit `2.9.2`،
runner `2.8.2`، Avalonia `11.3.19`) — بدون ترقيات ولا downgrades ولا
تعارضات.

README.md: شارة Cross-Platform CI مضافة في الأعلى.

---

## ✅ الإصلاحات السابقة (ملخّص)
- **Copy Name / Paste Name**: يلصق الاسم النصي فقط (يستخرج `Name` من JSON العقدة إذا كانت الحافظة تحمله) — Copy Node / Paste Node لم يتغيّرا.
- **زر الفأرة الأيمن** في قائمة العقدة: Copy / Cut / Paste / Copy Name / Paste Name / Delete تعمل.
- **Ctrl+V داخل مربع إعادة التسمية**: لا يُخطف من مربع النص (معالج مفاتيح واعٍ بالتركيز).
- **زر Save**: يفتح نافذة اختيار مكان الحفظ ويكتب الملف فقط عند التأكيد (Cancel لا يكتب شيئاً).
- **Sort**: فرز Recursive بأربعة خيارات.
- **حفظ تلقائي** عند إغلاق البرنامج مع سؤال المستخدم، و**فتح Save على آخر مجلد مستخدم**.

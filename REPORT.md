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

## ✅ الإصلاحات السابقة (ملخّص)
- **Copy Name / Paste Name**: يلصق الاسم النصي فقط (يستخرج `Name` من JSON العقدة إذا كانت الحافظة تحمله) — Copy Node / Paste Node لم يتغيّرا.
- **زر الفأرة الأيمن** في قائمة العقدة: Copy / Cut / Paste / Copy Name / Paste Name / Delete تعمل.
- **Ctrl+V داخل مربع إعادة التسمية**: لا يُخطف من مربع النص (معالج مفاتيح واعٍ بالتركيز).
- **زر Save**: يفتح نافذة اختيار مكان الحفظ ويكتب الملف فقط عند التأكيد (Cancel لا يكتب شيئاً).
- **Sort**: فرز Recursive بأربعة خيارات.
- **حفظ تلقائي** عند إغلاق البرنامج مع سؤال المستخدم، و**فتح Save على آخر مجلد مستخدم**.

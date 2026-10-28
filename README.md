# NoteApp — Productivity Suite

แอปจดบันทึกครบวงจร สำหรับวัยทำงานและเรียน สร้างด้วย WPF (.NET 8)

## ฟีเจอร์

| ฟีเจอร์ | รายละเอียด |
|---|---|
| **โน้ต** | สร้าง แก้ไข ลบ ปักหมุด ล็อครหัส แจ้งเตือน |
| **การเงิน** | รายรับ-จ่าย บิล นำเข้า SMS ธนาคาร ข้อมูลตลาด AI วิเคราะห์ |
| **ประชุม** | จองห้อง ตาราง Discord/Classroom integration |
| **Study Hub** | AI สรุปบทเรียน แฟลชการ์ด ข้อสอบ GPA Focus Timer |
| **Community** | กระดานโพสต์ ถามตอบ แชร์ผ่าน WiFi |
| **Cloud Sync** | GitHub Gist / Firebase sync ข้ามเครื่อง |
| **Dashboard** | สถิติ กราฟ สรุปภาพรวม |
| **ธีม** | 20 ธีม Dark/Light สลับได้ |

## ดาวน์โหลด

ไปที่ [Releases](../../releases) แล้วดาวน์โหลด `NoteApp-v2.0-win64.zip`

## วิธีใช้

1. ดาวน์โหลด zip
2. แตกไฟล์
3. รัน `NoteApp.exe`

> Windows 10/11 64-bit · ไม่ต้องติดตั้ง · ข้อมูลเก็บในเครื่อง

## Build จาก Source

```
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

## Tech Stack

- C# / WPF / .NET 8
- JSON file storage
- Firebase REST API / GitHub Gist API
- HTTP server สำหรับ Community sharing

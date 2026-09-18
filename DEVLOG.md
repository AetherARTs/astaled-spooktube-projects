# Dev Log — Solo revision 6 / 19 September 2026

งานต่อจากฟีดแบ็กล่าสุด: เน้นความมืด กล้องที่มีรายละเอียด และทางเข้าเกมที่ใช้งานจริง

- เพิ่ม Scene MainMenu และ Loading; เมนู Options, How to Play, Credits และ Pause เชื่อมกับเกมจริง
- Options ปรับเสียง ความไวเมาส์ FOV ความละเอียด เต็มจอ VSync และระดับกราฟิก; ค่าเริ่มต้น Balanced มี render scale 85%, เป้า 60 FPS
- Esc หยุดเกม Solo ได้; โทรศัพท์ยังใช้ขณะโลกเดินต่อ ป้องกัน Esc ปิดโทรศัพท์แล้วไปเปิด Pause ซ้ำในเฟรมเดียว
- กลับ Title บันทึก Career/คลิปก่อน หากบันทึกไม่ได้จะคงฉากเดิมไว้; ออกจากภารกิจนับเป็นจบรอบกู้คืนตามกติกาเดิม
- ปั้น MainCam ใหม่ใน Blender เป็นตัวถังโค้ง เลนส์/วงแหวนโฟกัส ไมค์ ช่องระบาย สกรู สายจับ และแบตเตอรี่ แยก LCD ภาพสด; เก็บต้นฉบับ `ArtSource/Environment/EQP_MainCam_Master.blend`
- โรงพยาบาลลด ambient/ไฟแวดล้อมและหมอกมืด ใช้ไฟฉายส่องเพื่อถ่ายเป้าหมาย; LCD ไม่เรนเดอร์ shadow map ซ้ำกับกล้องผู้เล่น
- แก้ไฟไหล่ถูกกล้อง/ตัวเองบังลำแสง และป้ายในโลกทะลุกล้อง/โทรศัพท์ด้วย depth-tested font shader
- เก็บฉาก v4/v5 สำหรับคลิปเดิม โดยตรึงตัวกล้องเก่าไว้ในฉากย้อนหลัง; ไม่สร้างตัวละครใหม่และไม่เพิ่มหน้า LED

การตรวจรอบนี้: Solo 62 ข้อ, Bobby 57 ข้อ, Menu/Pause 18 ข้อ และภาพทดสอบป้ายที่ถูกวัตถุบังผ่านแล้ว; Windows สร้างสำเร็จ 0 errors. รายงาน GPU/บิลด์ส่งมอบอยู่ใน WORK_STATE.md.

**ยังทำต่อ:** โรงพยาบาลสุ่มห้อง/ทางเดินพร้อมผังเก็บในคลิป, ขยายตามวันและโควตาที่ผ่าน, หลุม/เส้นทางปีนป่าย, อีกสามด่านตาม GDD และระบบ Solo ที่ยังเหลือทั้งหมด. ขณะนี้ด่านเล่นจริงยังเป็นโรงพยาบาลที่จัด layout ไว้ ไม่ใช่ procedural และยังไม่ใช่เกมทั้ง GDD ที่เสร็จครบ.

---

# Solo revision 5 / 18 September 2026

รอบนี้ทำตามฟีดแบ็กการเล่นจริงทั้งหมด โดยย้ายลำดับความสำคัญมา Solo, กล้อง, ฟิสิกส์, ไมค์, โรงพยาบาล และ UI ก่อน Co-op

## เสร็จและเชื่อมกับเกมแล้ว

- กล้องหมุนตามสายตาโดยไม่บิดตอนหันเร็ว คลิกขวายก LCD ใกล้แบบ blend; ปรับระยะจับและท่ามือ ลดการบังจอ
- ใช้โมเดลผู้เล่นเดิมทั้ง rig ย่อเหลือ 88% เสื้อเดิมและหน้าจอดำเดิม ไม่สร้างตัวละครใหม่
- วิ่ง กระโดด ย่อ สไลด์ ปีนขอบเตี้ยที่กำหนด เอียงหลบมุม ผลักวัตถุ; ป้องกันลุกทะลุเพดานและเปิดโทรศัพท์แล้วค้างกลางอากาศ
- ถือของ 3 ช่อง สลับเลข 1–3/วงล้อ วาง–เก็บได้; เปลี่ยนจากกล้องที่กำลังถ่ายจะบันทึกก่อน
- ถุงมือแรงโน้มถ่วงดึง/ประคอง/ผลัก Rigidbody จริง มีข้อจำกัดน้ำหนัก ระยะ สิ่งบัง และพลังงาน
- รับเสียงไมค์ Windows จริง มีเลือกอุปกรณ์ มิเตอร์ GATE และ Mute; ส่งเสียงพูดและเสียงเดิน/ประตู/ตกกระแทก/อุปกรณ์ล่อไปยัง AI
- โทรศัพท์ถือในมือ: Mission, แผนที่สำรวจแล้ว, Equipment, Harmony สำหรับ Solo, Settings และ Controls; แบตเตอรี่/ชาร์จที่บ้านและ RV
- UI ใช้ฟอนต์ Chakra Petch ที่มีใบอนุญาต ปุ่มมีสถานะเลือก คีย์บอร์ดเข้าแอป/ปิดเมนูเป็นลำดับ; ตรวจภาพทั้ง 16:9 และ 16:10
- Bobby มีตัวเลือกให้ตัดเอง หรือกดให้ Bobby ตัด–อัปโหลดในเกม–เปิดดูผลอัตโนมัติ; save ล้มเหลวไม่คิดรางวัลและลองใหม่ได้
- Credit เป็นเงินซื้อ/ขาย ส่วน View เป็นโควตา 3 วันแยกกัน; สัญญาผ่านแล้วเป้าเพิ่ม 35%, พลาดแล้ว Retry เก็บเงิน/ของเดิม; ป้องกันรับรางวัลซ้ำ
- บ้าน 30×30 ม. กับโรงรถ 16×14 ม. เพิ่มเฟอร์นิเจอร์ พื้นที่ทำงาน NPC Bobby/Supply Clerk และร้าน Work Light/Noisemaker
- โรงพยาบาล 37×68 ม. (2,516 ตร.ม.) ลานหน้า 47×24 ม. ห้องหลัก 8 ห้อง ทางเดินกว้าง 4–5 ม. มีทางวนและชุดพร็อพการแพทย์
- ประตูเลื่อนเข้าช่องกำแพง เปิดทางเดิน/เส้นทาง AI ได้จริง และไม่ปิดทับคนหรือวัตถุ
- ทำ asset เพิ่มใน Blender 34 ชิ้นกับ texture map ต้นฉบับ 21 ไฟล์ เก็บ .blend/Python/FBX ที่แก้ต่อได้
- Career v3 ย้ายข้อมูลเดิมได้ แยกฉาก Legacy v4 สำหรับเปิดคลิปเก่า; การเขียนเงิน/ของ/ใบเสร็จมี backup และไม่แก้ข้อมูลเมื่อเขียนไม่สำเร็จ

## ตรวจแล้ว

- SoloSystemsCheck: 62 ข้อผ่าน ครอบคลุม input/movement/phone/3 ช่อง/glove/เสียง/ประตู/เงิน/โควตา/กลับบ้าน
- Bobby production: 57 ข้อผ่าน; โหลดใหม่ 14 ข้อ; คลิปเก่า 4 ข้อ; Bobby อัตโนมัติและกันจ่ายซ้ำ 10 ข้อ
- UI คีย์บอร์ด/Dropdown/back: 8 ข้อผ่าน; ใช้ภาพจาก Unity ตรวจความอ่านง่ายและมือบังจอแยกจาก state test
- ไมค์จริง: 3 ข้อผ่านกับ Maono DM40 Wireless Mic ตรวจเปิดอุปกรณ์ ตัวชี้บัฟเฟอร์ และ sample จริง ไม่บันทึกเสียงพูดลงดิสก์
- กล้องหมุนต่อเนื่อง: จาก peak ผิดทิศ 179.95° เหลือ 0° ใน repro เดิม; เส้นทาง NavMesh ห้องโรงพยาบาลผ่าน
- Windows v5 เคยผ่าน smoke บน GTX 1050 Ti; บิลด์ส่งมอบปัจจุบันและผลตรวจล่าสุดดู WORK_STATE.md

รายงานอยู่ใน `QA/v5_*`, `QA/bobby_playmode_validation.txt`, `QA/bobby_reload_validation.txt` และ `QA/legacy_take_validation.txt` ไม่ได้นับยอด 192 ข้อของ revision 4 เป็นผลทดสอบรอบใหม่

## ยังไม่เสร็จ / รอบต่อไป

- งานภาพยังไม่ถึง near-realistic ที่ต้องการ: ต้องเก็บวัสดุ/พร็อพ/เรื่องเล่าในฉาก เงา รายละเอียดสถาปัตยกรรม แอนิเมชัน และประสิทธิภาพต่อ
- แผนที่ยังเป็น layout ที่จัดไว้หนึ่งโรงพยาบาล มีศัตรู Surgeon ชนิดเดียว; ชั้นบน/เส้นทางแนวตั้งเต็มรูปแบบ procedural layout ศัตรู/เหตุการณ์และภารกิจเพิ่มยังเหลือ
- ปีนได้เฉพาะขอบเตี้ยที่ทำไว้ ไม่ใช่ปีนทุกผิว; ตัวละครยังใช้ CharacterController + selective physics ไม่ใช่ active ragdoll เต็มตัว
- NPC มีการมอง/idle กับจุดใช้งาน ยังไม่มีตารางชีวิต บทสนทนา หรือการเดินเต็มระบบ
- คลิปละ 60 วินาที ตอนละหนึ่ง source take; ตัดข้ามหลายคลิป/กล้อง streaming ยาว และ export วิดีโอยังไม่ทำ
- ไมค์ใช้ตรวจเสียงเพื่อ AI ยังไม่ได้เก็บเสียงผู้เล่นในคลิป; ระดับ GATE ต้องปรับให้เหมาะกับเสียงพูดและห้องจริง
- UI ภาษาไทย, คอนเทนต์/เศรษฐกิจทั้ง GDD, บาลานซ์ระยะยาว และ Co-op ยังไม่เสร็จ; LED face เว้นตามคำสั่ง

วิธีเล่น: [HOW_TO_PLAY_TH.md](HOW_TO_PLAY_TH.md) • ระบบ/ค่าปัจจุบัน: [PRODUCTION_README.md](PRODUCTION_README.md)

---

# ประวัติรอบก่อน — ขอบเขตและสถานะด้านล่างเป็นของ revision 4 หรือต่ำกว่า
# SPOOKYTUBER — Dev Log

อัปเดต: **18 กันยายน 2026** · รุ่นงานปัจจุบัน: **Revision 4** · โค้ดที่สรุป: [e8e5ee9](https://github.com/AetherARTs/astaled-spooktube-projects/commit/e8e5ee99ec457fbfe57a6763537df3cb6f3e3d7f)

เทียบ scope กับ `SpookTuber_Master_GDD_v0.5` และ `SPOOKYTUBER_Development_Plan_TH` พร้อมตรวจเอกสารส่งมอบและรายงาน QA ใน repository

ตอนนี้วงจรหลักแบบ **Solo** ทำงานเชื่อมกันแล้ว:

**บ้าน → ออกกองที่โรงพยาบาล → ถ่าย Surgeon → กลับ RV → Bobby ตัดต่อ → ดู Episode จริง → อัปโหลดในเกม → รับเงิน/ผู้ติดตาม → ซื้ออัปเกรด**

คำว่า “เสร็จ” ในบันทึกนี้หมายถึงใช้งานได้ในขอบเขตรุ่นปัจจุบัน งานทั้งหมดอยู่ในโปรเจกต์ผลิตจริง แต่ยังไม่ครบ CORE หรือ V1 ตาม GDD และยังรอผลเล่นทดสอบจากผู้ใช้ ไม่ระบุเปอร์เซ็นต์ความสำเร็จของทั้งเกมจากจำนวนฟีเจอร์หรือจำนวน checks

## 1. งานที่เสร็จและใช้งานได้แล้ว

| หมวด | สิ่งที่ทำได้ในรุ่นนี้ |
| --- | --- |
| ตัวละครหลักใน Blender | มีหุ่นเปล่าและชุด Hoodie ชุดแรก แก้ทรงแขนเสื้อ ไหล่ ข้อศอก ข้อมือ จั๊มพ์และตะเข็บ พร้อมไฟล์ `.blend` ที่แก้ต่อได้ |
| ใบหน้า | เป็นจอสีดำว่างตามคำสั่งผู้ใช้ เอาหน้า/ตาเดิมออกแล้ว |
| Rig และแอนิเมชัน | Humanoid 59 กระดูก รวมมือและนิ้ว มี Idle/Walk/Run, จุดติดอุปกรณ์ และตรวจการงอข้อศอกใน Blender |
| ฟิสิกส์ตัวละคร | Ragdoll 11 bodies / 15 colliders, หัวหลุดที่เคลื่อนที่ได้ในขอบเขตและใช้พลังงาน, ฟังก์ชันคืนร่าง และการแกว่งของเครื่องประดับ |
| การควบคุม | เดิน วิ่ง กระโดด มองรอบตัว มุมมองบุคคลที่หนึ่ง/สาม ไฟไหล่ และเปลี่ยนระหว่างหุ่นเปล่ากับ Hoodie |
| MainCam | หยิบ/วางกล้องจริง จับด้วยสองมือผ่าน IK และจัดนิ้ว มีจอ LCD แสดงภาพจากเลนส์ ตรวจการบังของผนัง กล้องที่วางลงยังถ่ายต่อได้ |
| การบันทึก | เก็บท่าทางโลกและกระดูก การมองเห็น แสง และเสียงในเกม บันทึก/โหลด `.sttake` ได้ ถ้าเขียนไม่สำเร็จจะเก็บคลิปในหน่วยความจำให้ลองใหม่ |
| Replay | เล่นภาพเคลื่อนไหวจากเลนส์และสถานะที่บันทึกจริง พร้อมเสียงในเกม หยุด/เลื่อนได้ โดยไม่จำลอง AI ฟิสิกส์ หรือแจก reward ซ้ำ |
| Hospital | มีฉากโรงพยาบาล 6 ห้อง ทางเชื่อม ทางเดิน ประตู และจุด RV พร้อมชุดโมเดล Blender/FBX 21 ชิ้น รวม props, Surgeon และ RV |
| Surgeon | เดินตรวจ สนใจเสียง เตือน ไล่ตาม ค้นหาตำแหน่งที่เห็นล่าสุด ง้างโจมตี พักหลังโจมตี และฝืนเปิดประตูเพื่อเดินผ่านได้ |
| Mission loop | ออกจากบ้าน เข้า Hospital กลับขึ้น RV พร้อมนับถอยหลังที่ยกเลิกได้ กู้คืนอุปกรณ์เริ่มต้น และกลับบ้านอัตโนมัติหลังล้มใน Solo |
| หลักฐานที่ถ่ายได้ | ตรวจว่ากล้องเห็น Surgeon จริง อยู่ในเฟรม มีแสงและไม่มีผนังบัง เชื่อมกับสถานะจริง เช่น Sighting, Warning, Pursuit และ Attack |
| Bobby workstation | เลือกเทปที่นำกลับมาได้ ตัดอัตโนมัติแบบ Documentary/Horror เก็บบริบทก่อน–หลังเหตุการณ์ และดู Episode ที่เคลื่อนไหวจริง |
| แก้คลิปด้วยมือ | ปรับหัว/ท้ายทีละครึ่งวินาที เรียง/ลบช็อตจากชุดที่สร้างไว้ Undo/Redo ดูเทปต้นฉบับ บันทึก draft และเปิดฉบับที่เผยแพร่ไว้ตรงตามเดิม |
| อัปโหลดและรายได้ | เผยแพร่บน SpookTuber TV ภายในเกม ได้ views, subscribers และ revenue มีความคิดเห็นที่อ้างอิงเหตุการณ์ใน Episode แยกเงินทีม/เงินผู้เล่น |
| อัปเกรด | ซื้อ Context Editor ของ Bobby ด้วยเงินทีม เปลี่ยนบริบทและการเลือกช็อตจริง พร้อมเพิ่มงบความยาว Episode |
| Career save | เก็บเทป draft ฉบับเผยแพร่ เงิน ผู้ติดตาม และอัปเกรด มี checksum, revision และ backup ตรวจการจ่ายซ้ำและการซื้อซ้ำ ถ้าเซฟไม่สำเร็จไม่หัก/เพิ่มเงินหรือให้ของ |
| Git | Push source, Blender, Unity assets พร้อม `.meta`, dependency manifests, เอกสารและหลักฐาน QA ขึ้น `master` แล้ว แก้ `.gitignore` ที่เคยกันไฟล์แพ็กเกจ Unity ออก |

## 2. ทำแล้วบางส่วน — ยังมีข้อจำกัด

| ระบบ | ขอบเขตที่มีอยู่ | ส่วนที่ยังต้องทำ |
| --- | --- | --- |
| ตัวละคร/เสื้อผ้า | ใช้ใน Unity ได้ มี UV, material, skinning และ physics พื้นฐาน | เก็บงานภาพและท่าทางเพิ่ม, texture atlas, LOD และวัด performance; เสื้อเป็น skinned mesh ยังไม่มี cloth simulation แบบอิสระ |
| Footage | MainCam ตัวเดียว สูงสุด **60 วินาทีต่อ take**; หลาย take เก็บอยู่ในรายการได้ | บันทึกรอบยาวแบบ streaming และตัดต่อข้าม take/หลายกล้อง |
| Episode | ใช้ **หนึ่ง take ต่อ Episode**, สูงสุด 8 ช็อต; เผยแพร่ได้ **หนึ่ง Episode ต่อ expedition** | ขยาย source bin, มุมกล้องและเหตุการณ์ข้ามช่วง รวมถึง workflow manual edit ที่ครบขึ้น |
| Metadata | ประเมินภาพจากจุดตรวจสามจุด แสง/หมอกของฉาก และสถานะ Surgeon | Event family, subject และ sensor mode อื่น ๆ; การประเมินปัจจุบันไม่ใช่การวิเคราะห์ทุก pixel |
| Replay | รองรับตัวละคร/วัตถุที่มีอยู่ตอนเริ่มบันทึก | วัตถุที่ spawn ภายหลัง, material/VFX state ทั่วไป และการส่งออกวิดีโอ |
| Hospital | Layout ที่สร้างไว้ตายตัว พร้อมทางเดินและ NavMesh | Procedural modular generation, seed/manifest และการตรวจเส้นทางบน layout จำนวนมาก |
| ศัตรู | Surgeon ทำงานได้หนึ่งแบบ | Peeker, Clinger, encounter/counterplay ของแต่ละตัว และตัวควบคุมจังหวะความกดดันโดยรวม |
| หัวหลุด/การช่วยเหลือ | มีหัวหลุดและ Solo cloud recovery; มีฟังก์ชัน Repair | ผู้เล่นช่วยกันแบกร่าง/กู้คืนร่าง และสถานีซ่อมที่เป็น gameplay ครบระบบ |
| RV | เป็นจุดออกกองและ extraction จอดอยู่กับที่ | ขับ RV, camera station, medical/repair station และ drone delivery |
| Bobby | ใช้งานผ่านโต๊ะตัดต่อ มีอัปเกรดหนึ่งระดับ | โมเดล NPC Bobby เฉพาะตัว แอนิเมชัน/ปฏิกิริยา และ upgrade tree ตาม GDD |
| เศรษฐกิจ | เงินใน career เดียว สูตร views/revenue ตั้งต้น แบ่งทีม/ผู้เล่น 60/40 | บาลานซ์จริง การลดค่าคอนเทนต์ซ้ำข้ามรอบ ร้านค้าเต็มระบบ สัดส่วนที่ host ตั้งได้ และสิทธิ์รายได้ของทีมหลายคน |
| เสียง/UI | มีเสียงกลไก ฝีเท้า ประตู ศัตรู ambience และ UI เล่น/ตัดต่อ | Sound design/mix ครบเกม, signal interference, ภาษาไทย และการตรวจ accessibility/หลาย resolution เพิ่ม |

ราคาปัจจุบันของ Context Editor คือ **25.00 เงินทีม** และงบความยาว Episode เปลี่ยนจาก **24 เป็น 36 วินาที** ตัวเลขเหล่านี้รวมถึงสูตรรายได้เป็นค่าตั้งต้นสำหรับ playtest ไม่ใช่บาลานซ์ที่ล็อกใน GDD

การอัปโหลดเป็นการเผยแพร่ **ภายในเกมบนเครื่องเดียว** ยังไม่มีบริการออนไลน์ ไม่มีการอัปโหลดวิดีโอไปแพลตฟอร์มจริง และไม่ได้บันทึกเสียงไมโครโฟน

## 3. งานที่ยังไม่เสร็จตาม GDD/แผน

**งาน CORE ที่ยังต้องปิด**

- Co-op 1–6 คน: lobby/join, host/client authority, sync ผู้เล่น–อุปกรณ์–ฟิสิกส์–ศัตรู, หลุด/กลับเข้าเกม และการทดสอบหลายเครื่อง
- Cut Board และ Scene Intent ที่เชื่อมกับเหตุการณ์/คลิปที่ยืนยันแล้ว
- กล้องเสริมและการดู feed หลายกล้อง รวมถึงการตัดต่อจากหลายแหล่ง
- Hospital แบบ procedural modular rooms และการตรวจ connectivity ของห้อง/ประตู/ทางหนี
- Peeker และ Clinger เพื่อให้ครบอย่างน้อยสาม archetypes ที่สร้างเหตุการณ์ต่างกัน
- Carry/interaction ที่ครอบคลุมการช่วยเพื่อนและอุปกรณ์อื่นนอกจาก MainCam รวมถึง body rescue/return
- Smartphone พื้นฐาน: Mission, Map/GPS, Equipment และเส้นทางใช้งาน Harmony ตามแผน
- Horror audio และ signal interference ที่เชื่อมกับ gameplay
- Playtest แบบทีม การบาลานซ์ 1–6 คน และ performance/stress test สำหรับรอบจริง

**งาน V1 และคอนเทนต์ที่ยังเหลือ**

- อีกสาม Limbo: Weapon Manufacturing Facility, Flickerhorm AETHER Academy และ Haunted Mansion
- Roster ศัตรู/วัตถุอันตราย/กับดักเพิ่มเติม และ Found Footage พร้อมเงื่อนงำที่ผู้เล่นค้นพบได้
- Camera Modules เช่น IR / Night Vision / Thermal และอุปกรณ์ production อื่นตามแผน
- Production House แบบครบพื้นที่: equipment room, ห้องตัดต่อ Bobby, ห้องนอน/พื้นที่ส่วนตัว, shop และ garage
- ระบบ RV stations, การขับรถและ drone delivery
- Bobby upgrades ขั้นต้นถึงกลาง และ manual editing ที่รองรับ Scene/Angle/Clip Order ครบขึ้น
- Shop, cosmetics, หัว/เสื้อผ้าชุดอื่น การตกแต่งบ้าน และการใช้เงินส่วนตัว
- Harmony text chat/TTS; ระบบไมโครโฟนหรือ voice recording ต้องออกแบบและทดสอบแยกหากจะเพิ่ม
- Viral/trend simulation, advertising, old-video lifecycle/income และ community memory ที่อ้างอิง archive จริง
- งานศิลป์ แอนิเมชัน เสียง UX ภาษาไทย optimization และการทดสอบเครื่องเป้าหมายก่อนส่งมอบ V1

**พักไว้ตามคำสั่งผู้ใช้:** ใบหน้า LED เช่น `^^`, `><`, `o o` ยังไม่ทำ จอตัวละครหลักต้องคงเป็นสีดำว่างในระหว่างนี้

ยังไม่ประกาศผ่าน CORE: ปัจจุบันมีผู้เล่นหนึ่งคน ศัตรูหนึ่ง archetype และแผนที่ตายตัว ส่วนที่เหลือข้างต้นยังอยู่ใน scope ไม่ได้ถูกตัดออกเพราะยังไม่มี implementation

## 4. ผลตรวจที่มีหลักฐาน

| ชุดตรวจ | จำนวน checks | ผล |
| --- | ---: | --- |
| ตัวละคร/กล้อง/ฟิสิกส์/การบันทึก | 60 | PASS |
| โหลดคลิปบ้านใน Unity process ใหม่ | 4 | PASS |
| Hospital mission loop / perception / extraction / recovery | 48 | PASS |
| โหลดคลิปโรงพยาบาลและเสียงใหม่ | 3 | PASS |
| Surgeon ฝืนประตูและเดินผ่านจริง | 5 | PASS |
| Evidence → Bobby → upload / wallet / purchase | 54 | PASS |
| Career reload / EDL / cut boundary / save recovery | 14 | PASS |
| เปิดคลิปรุ่นก่อนและไม่เขียนทับโดยไม่จำเป็น | 4 | PASS |
| **รวม** | **192** | **PASS** |

- Windows development build: **Succeeded, 0 build errors**
- เปิด executable ด้วย GPU จริงบน GTX 1050 Ti เป็นเวลา **115.7 วินาที** ไม่พบ managed/game exception หรือ asset-load failure ใน log ที่ตรวจหลังปิดโปรแกรม
- วงจรเล่นเต็มรอบตรวจใน **Unity Play Mode** ส่วน executable ตรวจการเปิดทำงาน ยังไม่ใช่รายงาน FPS หรือผลเล่นเต็มรอบบน standalone
- ยังรอผล playtest จากผู้ใช้ โดยเฉพาะความสนุก ความชัดของภาพ จังหวะ Surgeon ความเข้าใจหน้าตัดต่อ และความเหมาะสมของรายได้/ราคา
- Unity Editor เคยมี startup indexing exception ของ `UnityEditor.Search` แยกจากชุดตรวจเกม จึงไม่กล่าวว่า Editor log ทุกไฟล์ไม่มี error

หลักฐาน: [สรุป 192 checks](QA/v4_validation_summary.json), [ผล build](QA/standalone_build.txt), [ผลเปิด executable](QA/standalone_runtime_check.txt), [บันทึกปัญหาและการแก้ revision 4](QA/v4_debug_ledger.md)

ภาพจากแอปจริง: [ตัวละครและแขนเสื้อ](QA/Player_Hoodie_ElbowCheck.png), [Hospital](QA/Hospital_Corridor.png), [หน้าตัดต่อ](QA/Bobby_Edit.png), [ผลอัปโหลด](QA/Bobby_Upload.png)

## 5. ประวัติงานที่ส่งมอบ

| ช่วงงาน | ผลที่ส่งมอบ |
| --- | --- |
| Character revision 2 | ปรับ Hoodie/แขนเสื้อ รักษาหน้าจอดำ ริก แอนิเมชัน ฟิสิกส์ และ MainCam recording/replay |
| Hospital revision 3 | ชุดโมเดลโรงพยาบาล Surgeon, RV, hand grip/LCD, เสียงในคลิป และวงจรออกกอง–กลับบ้าน; ผ่าน 120 checks ณ ตอนส่งมอบ |
| Production revision 4 | Filmed evidence, Bobby editing/episode playback, upload, career wallets, upgrade และความเข้ากันได้กับคลิปเก่า; รวมล่าสุด 192 checks |
| Git checkpoint — 18 ก.ย. 2026 | Push [e8e5ee9](https://github.com/AetherARTs/astaled-spooktube-projects/commit/e8e5ee99ec457fbfe57a6763537df3cb6f3e3d7f) ขึ้น `master` ครบ 279 ไฟล์ที่เปลี่ยนใน checkpoint รวม package manifests ที่จำเป็น |

ต่อจากนี้ commit งานที่จบเป็นช่วง ๆ และ push หลังตรวจผ่าน ตามคำสั่งผู้ใช้

## 6. จุดเริ่มงานรอบถัดไป

1. รับผลเล่นจริงของ build ปัจจุบัน แก้ blocker และปัญหาการควบคุม/ภาพ/เสียงก่อนเพิ่ม scope
2. ขยาย recorder และ episode ให้รองรับรอบยาว หลาย take และหลายกล้อง พร้อม Cut Board
3. วางและทดสอบ Co-op authority/sync ควบคู่กับ interaction, carry และ body rescue ก่อนขยายระบบที่ต้องใช้ร่วมกัน
4. เพิ่ม Peeker/Clinger, procedural Hospital และ phone/signal เพื่อปิดส่วน CORE ที่ยังขาด
5. เก็บงาน Bobby/บ้าน/ตัวละคร บาลานซ์เศรษฐกิจ และวัด performance จากการเล่นทั้งรอบ

ลำดับนี้เป็นรายการทำงานถัดไปสำหรับทบทวนจากผลเทส ไม่ใช่กำหนดวันเปิดขาย

## 7. ไฟล์สำหรับเปิดงานและทดสอบ

- เกม Windows บนเครื่องผู้ใช้: `D:/Projects/SPOOKYTUBER/Builds/Windows/SpookTuber.exe`
- Unity project: [My project](My%20project) — Unity **6000.3.18f1**; เริ่มที่ `Assets/SpookTuber/Scenes/ProductionHouse.unity`
- โมเดลหลัก: [CHR_Player_Master.blend](ArtSource/Player/CHR_Player_Master.blend)
- ชุดโรงพยาบาล: [Hospital_AssetLibrary.blend](ArtSource/Hospital/Hospital_AssetLibrary.blend)
- วิธีเล่น/ข้อจำกัดการตัดต่อและเศรษฐกิจ: [PRODUCTION_README.md](PRODUCTION_README.md)
- รายละเอียดตัวละครและโรงพยาบาล: [CHARACTER_README.md](CHARACTER_README.md), [HOSPITAL_README.md](HOSPITAL_README.md)
- บันทึกส่งต่องาน: [WORK_STATE.md](WORK_STATE.md)

Git เก็บ source/assets/เอกสาร/หลักฐาน QA ส่วน executable, Unity cache, local logs และ Blender backup ไม่อยู่ใน repository

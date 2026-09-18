import bpy, math, json, random, wave, struct
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Unity/Assets/SpookTuber/Hospital/Models'
QA=ROOT/'QA'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
palette={
 'H_Plaster':(.36,.39,.34),'H_GreenPaint':(.12,.22,.19),'H_Tile':(.38,.43,.39),
 'H_Grout':(.055,.065,.057),'H_Steel':(.22,.27,.26),'H_DarkMetal':(.035,.047,.045),
 'H_Rust':(.23,.095,.044),'H_Linen':(.43,.42,.31),'H_Coat':(.48,.47,.36),
 'H_Rubber':(.016,.021,.023),'H_Glass':(.018,.068,.055),'H_Light':(.7,.78,.58),
 'H_Red':(.36,.035,.018),'H_Paper':(.61,.57,.43),'H_Amber':(.63,.37,.10)}
mats={}
for name,color in palette.items():
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
 bs.inputs['Roughness'].default_value=.7 if name in ['H_Linen','H_Coat','H_Plaster'] else .43
 bs.inputs['Metallic'].default_value=.7 if 'Metal' in name or name=='H_Steel' else 0
 mats[name]=m
parts=[];assets=[];stats={}
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import asset_tools as kit
for key in ['ROOT','OUT','QA','scene','mats','parts','assets','stats']:setattr(kit,key,globals()[key])
from asset_tools import finish,box,cylinder,export,legs,wheels

# Every asset is authored at real scale around a reusable origin.
box('Wall',(0,0,1.5),(4,.16,3),'H_Plaster')
box('Lower paint',(0,-.09,.64),(4,.02,1.28),'H_GreenPaint',.002)
box('Kick rail',(0,-.11,.16),(4,.035,.16),'H_DarkMetal',.004)
box('Chair rail',(0,-.115,1.25),(4,.045,.055),'H_Steel',.004)
export('H_Wall_4m')
box('Slab',(0,0,-.08),(4,4,.16),'H_Grout',0)
for x in range(8):
 for y in range(8):box('Tile',(-1.75+x*.5,-1.75+y*.5,.002),(.486,.486,.025),'H_Tile',.003)
export('H_Floor_4m')
box('Ceiling',(0,0,0),(4,4,.12),'H_Plaster',0);export('H_Ceiling_4m')
for x in [-.88,.88]:box('Jamb',(x,0,1.22),(.1,.28,2.44),'H_DarkMetal')
box('Lintel',(0,0,2.46),(1.86,.28,.12),'H_DarkMetal');export('H_DoorFrame')
# Door origin is on its hinge, opened by the Unity hinge transform.
box('Door',(.8,0,1.19),(1.6,.075,2.38),'H_GreenPaint')
box('Recess',(.8,-.043,.66),(1.38,.02,.95),'H_Steel',.012)
box('Viewing glass',(.8,-.043,1.76),(.70,.018,.58),'H_Glass')
for x in [.42,1.18]:box('Window border',(x,-.06,1.76),(.035,.02,.65),'H_DarkMetal',.002)
for z in [1.45,2.07]:box('Window border',(.8,-.06,z),(.8,.02,.035),'H_DarkMetal',.002)
for y in [-.08,.08]:cylinder('Handle',(1.41,y,1.01),(1.41,y,1.19),.022)
box('Kick plate',(.8,-.047,.2),(1.43,.02,.25),'H_DarkMetal');export('H_Door')

box('Bed frame',(0,0,.49),(1,2.1,.12),'H_Steel')
box('Mattress',(0,0,.63),(.93,1.95,.20),'H_Linen',.055)
box('Pillow',(0,.67,.77),(.67,.39,.14),'H_Paper',.065)
for y in [-1.01,1.01]:
 for x in [-.45,.45]:cylinder('Upright',(x,y,.24),(x,y,1.02),.026)
 cylinder('Head rail',(-.45,y,1.02),(.45,y,1.02),.026)
 for x in [-.28,0,.28]:cylinder('Bars',(x,y,.54),(x,y,.96),.017)
wheels(.82,1.7);export('H_Bed')

for z in [.19,.62,.91]:box('Tray',(0,0,z),(.68,.43,.035),'H_Steel')
legs(.59,.34,.92);wheels(.59,.34)
cylinder('Push bar',(-.31,.21,1.03),(.31,.21,1.03),.021)
box('Gauze box',(-.16,0,.97),(.2,.23,.11),'H_Paper')
for x in [0,.1,.2]:cylinder('Vial',(x,0,.93),(x,0,1.08),.029,'H_Glass')
export('H_MedicalCart')

box('Cabinet',(0,0,.89),(.88,.43,1.78),'H_GreenPaint',.02)
for x in [-.22,.22]:
 box('Door panel',(x,-.23,.9),(.408,.04,1.64),'H_Steel')
 box('Glass panel',(x,-.256,1.22),(.33,.016,.88),'H_Glass')
 cylinder('Handle',(x*.3,-.285,.75),(x*.3,-.285,.95),.015)
export('H_MedicineCabinet')

box('Seat',(0,0,.45),(.53,.49,.075),'H_GreenPaint',.028)
box('Back',(0,.215,.79),(.53,.07,.55),'H_GreenPaint',.028)
legs(.43,.39,.45);export('H_Chair')
for x in [-.67,0,.67]:
 box('Seat',(x,0,.46),(.62,.51,.09),'H_GreenPaint',.035)
 box('Back',(x,.22,.77),(.62,.09,.55),'H_GreenPaint',.035)
for x in [-.78,.78]:
 cylinder('Bench leg',(x,0,.04),(x,0,.43),.045)
 cylinder('Bench foot',(x,-.24,.045),(x,.24,.045),.035)
export('H_WaitingBench')

box('Locker',(0,0,.96),(.6,.55,1.92),'H_GreenPaint',.015)
box('Panel',(0,-.287,.97),(.54,.025,1.83),'H_Steel')
for z in [1.59,1.65,1.71]:box('Vent',(0,-.305,z),(.34,.008,.017),'H_DarkMetal',.001)
cylinder('Handle',(.2,-.33,.92),(.2,-.33,1.08),.016);export('H_Locker')

cylinder('Pole',(0,0,.15),(0,0,1.85),.022)
for i in range(5):
 a=i*math.tau/5;x,y=.32*math.cos(a),.32*math.sin(a)
 cylinder('Base',(0,0,.12),(x,y,.08),.022)
 cylinder('Caster',(x-.022,y,.05),(x+.022,y,.05),.043,'H_Rubber')
cylinder('Top',(-.23,0,1.84),(.23,0,1.84),.016)
box('Saline bag',(.17,0,1.59),(.13,.055,.24),'H_Glass',.025)
cylinder('Tube',(.17,0,1.47),(.12,0,.75),.004,'H_Rubber');export('H_IVStand')

box('Base',(0,0,.09),(.72,.62,.18),'H_DarkMetal',.05)
cylinder('Hydraulic',(0,0,.15),(0,0,.71),.14)
box('Table',(0,0,.84),(.77,1.92,.19),'H_GreenPaint',.03)
for x in [-.4,.4]:cylinder('Rail',(x,-.84,.77),(x,.84,.77),.028)
box('Headrest',(0,.91,.92),(.48,.38,.13),'H_Rubber',.035);export('H_OperatingTable')

cylinder('Ceiling mount',(0,0,2.7),(0,0,3),.12)
cylinder('Arm',(0,0,2.75),(.5,0,2.75),.055)
cylinder('Arm',(.5,0,2.75),(.8,0,2.35),.044)
cylinder('Lamp head',(.8,0,2.22),(.8,0,2.38),.31,'H_GreenPaint',24)
for i in range(6):
 a=i*math.tau/6;x=.8+.19*math.cos(a);y=.19*math.sin(a)
 cylinder('Reflector',(x,y,2.20),(x,y,2.22),.072,'H_Light')
export('H_SurgicalLamp')

box('Monitor body',(0,0,.26),(.46,.3,.39),'H_Linen',.028)
box('Screen',(0,-.157,.28),(.34,.014,.25),'H_Glass',.012)
for x in [-.14,-.07,0,.07,.14]:cylinder('Button',(x,-.16,.115),(x,-.18,.115),.015,'H_Steel')
box('Stand',(0,0,.035),(.38,.28,.07),'H_DarkMetal');export('H_Monitor')

for x in [-.72,.72]:
 cylinder('Post',(x,0,.05),(x,0,1.75),.023)
 cylinder('Foot',(x,-.26,.065),(x,.26,.065),.028)
cylinder('Rail',(-.72,0,1.75),(.72,0,1.75),.023)
for i in range(12):box('Curtain fold',(-.66+i*.12,(-1)**i*.025,1.03),(.125,.035,1.36),'H_Linen',.013)
export('H_PrivacyScreen')

box('Fixture',(0,0,.04),(1.25,.20,.08),'H_DarkMetal')
for y in [-.05,.05]:cylinder('Tube',(-.55,y,-.015),(.55,y,-.015),.022,'H_Light')
export('H_Fluorescent')
for x in [-.23,.23]:box('Rail',(x,0,.03),(.035,.45,.06),'H_Rust')
box('Box',(0,0,.22),(.5,.46,.42),'H_GreenPaint')
box('Lid',(0,0,.44),(.53,.49,.05),'H_Steel');export('H_SupplyCrate')
random.seed(310)
for i in range(10):
 o=box('Discarded paper',(random.uniform(-.65,.65),random.uniform(-.6,.6),.008+i*.001),(.19,.27,.004),'H_Paper',0);o.rotation_euler.z=random.uniform(-3,3)
export('H_PaperDebris')

# Crew production van, with a usable rear work panel rather than a placeholder block.
box('Underbody',(0,0,.47),(2.35,4.95,.24),'H_DarkMetal',.10)
box('Coach',(0,.4,1.59),(2.5,3.85,2.18),'H_Linen',.14)
box('Cab',(0,-1.91,1.33),(2.42,1.12,1.62),'H_Linen',.12)
box('Bonnet',(0,-2.51,.87),(2.35,.48,.46),'H_Steel',.10)
windshield=box('Windscreen',(0,-2.49,1.71),(2.05,.05,.81),'H_Glass',.04);windshield.rotation_euler.x=-.12
for s in [-1,1]:
 box('Cab window',(s*1.223,-1.91,1.72),(.027,.83,.66),'H_Glass',.025)
 for y in [-.38,1.04]:box('Coach window',(s*1.26,y,1.93),(.025,1.08,.62),'H_Glass',.025)
 box('Side stripe',(s*1.263,.38,1.1),(.025,3.6,.16),'H_Red',.005)
 box('Mirror',(s*1.42,-2.01,1.66),(.12,.18,.25),'H_DarkMetal',.02)
 cylinder('Mirror arm',(s*1.20,-1.96,1.49),(s*1.43,-1.96,1.54),.025)
 for y in [-1.65,1.45]:
  cylinder('Tyre',(s*1.18,y,.45),(s*1.40,y,.45),.43,'H_Rubber',24)
  cylinder('Wheel hub',(s*1.405,y,.45),(s*1.42,y,.45),.24,'H_Steel',16)
 box('Rear light',(s*.96,2.34,1.04),(.18,.055,.34),'H_Red',.025)
 box('Headlamp',(s*.84,-2.77,.96),(.40,.055,.23),'H_Light',.02)
box('Roof cap',(0,.41,2.72),(2.5,3.8,.13),'H_Steel',.06)
box('Vent',(0,.6,2.84),(.86,.65,.15),'H_GreenPaint',.04)
for x in [-.47,.47]:
 box('Rear door',(x,2.337,1.62),(.91,.035,1.69),'H_Steel',.01)
 box('Rear window',(x,2.36,2.01),(.73,.028,.52),'H_Glass',.015)
 cylinder('Latch',(x*.3,2.39,1.28),(x*.3,2.39,1.49),.022)
box('Rear step',(0,2.5,.39),(1.8,.38,.12),'H_DarkMetal',.03)
box('Rear bumper',(0,2.39,.61),(2.55,.16,.18),'H_DarkMetal',.045)
box('Front grille',(0,-2.765,.66),(1.45,.06,.21),'H_DarkMetal',.02)
for x in [-.5,-.25,0,.25,.5]:box('Grille',(x,-2.802,.66),(.045,.01,.19),'H_Steel',.002)
export('H_CrewRV')

# The Surgeon: a lean, jointed silhouette, split coat tails and an empty iron cage.
# Separate rigid pieces follow named bones; the coat is weighted through hip and torso.
armdata=bpy.data.armatures.new('SurgeonSkeleton');rig=bpy.data.objects.new('SurgeonRig',armdata);scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
spec=[('Root',(0,0,0),(0,0,.2),None),('Hips',(0,0,1.15),(0,0,1.35),'Root'),('Spine',(0,0,1.35),(0,0,1.78),'Hips'),('Head',(0,0,1.84),(0,0,2.23),'Spine')]
for side,s in [('L',1),('R',-1)]:
 spec += [(side+'UpperArm',(s*.27,0,1.77),(s*.36,-.025,1.29),'Spine'),(side+'Forearm',(s*.36,-.025,1.29),(s*.4,-.09,.84),side+'UpperArm'),(side+'Hand',(s*.4,-.09,.84),(s*.4,-.12,.65),side+'Forearm'),(side+'Thigh',(s*.12,0,1.16),(s*.12,0,.66),'Hips'),(side+'Shin',(s*.12,0,.66),(s*.12,0,.12),side+'Thigh')]
for name,head,tail,parent in spec:
 b=armdata.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=armdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
def weight(bone,begin):
 for o in parts[begin:]:
  g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
  m=o.modifiers.new('Surgeon skin','ARMATURE');m.object=rig;o.parent=rig
begin=len(parts)
box('Torso',(0,0,1.53),(.36,.23,.57),'H_Rubber',.04)
for s in [-1,1]:
 coat=box('Coat breast',(s*.15,-.025,1.51),(.15,.29,.58),'H_Coat',.025);coat.rotation_euler.y=s*.07
 lapel=box('Lapel',(s*.095,-.183,1.66),(.10,.027,.30),'H_Linen',.009);lapel.rotation_euler.y=-s*.23
 box('Pocket',(s*.175,-.18,1.30),(.10,.028,.12),'H_Linen',.008)
weight('Spine',begin)
begin=len(parts)
for s in [-1,1]:
 tail=box('Coat tail',(s*.17,.015,1.02),(.25,.3,.44),'H_Coat',.015);tail.rotation_euler.y=s*.09
box('Waist',(0,0,1.15),(.29,.23,.15),'H_Rubber',.02);weight('Hips',begin)
begin=len(parts)
box('Head void',(0,0,2.04),(.22,.17,.28),'H_Rubber',.015)
for x in [-.17,.17]:
 for y in [-.14,.14]:cylinder('Cage corner',(x,y,1.85),(x,y,2.27),.018,'H_Rust')
for z in [1.85,1.96,2.16,2.27]:
 for y in [-.14,.14]:cylinder('Cage',(-.17,y,z),(.17,y,z),.012,'H_Steel')
 for x in [-.17,.17]:cylinder('Cage',(x,-.14,z),(x,.14,z),.012,'H_Steel')
for x in [-.095,0,.095]:cylinder('Face bars',(x,-.145,1.86),(x,-.145,2.26),.012,'H_Rust')
for x in [-.056,.056]:box('Ember',(x,-.1,2.065),(.022,.013,.018),'H_Red',.003)
weight('Head',begin)
for side,s in [('L',1),('R',-1)]:
 begin=len(parts);cylinder('Sleeve',(s*.27,0,1.77),(s*.36,-.025,1.29),.077,'H_Coat');weight(side+'UpperArm',begin)
 begin=len(parts);cylinder('Sleeve',(s*.36,-.025,1.29),(s*.4,-.09,.9),.063,'H_Coat');cylinder('Wrist',(s*.4,-.09,.92),(s*.4,-.09,.81),.04,'H_Rubber');weight(side+'Forearm',begin)
 begin=len(parts);box('Glove',(s*.4,-.105,.76),(.095,.10,.18),'H_Rubber',.022)
 for i in range(4):cylinder('Finger',(s*.4+(i-1.5)*.022,-.12,.71),(s*.4+(i-1.5)*.022,-.15,.63),.011,'H_Rubber')
 if side=='R':
  cylinder('Tool handle',(s*.4,-.1,.72),(s*.4,-.38,.72),.029,'H_Rubber')
  box('Surgical blade',(s*.4,-.49,.61),(.025,.24,.26),'H_Steel',.008)
 weight(side+'Hand',begin)
 begin=len(parts);cylinder('Trouser',(s*.12,0,1.15),(s*.12,0,.66),.072,'H_DarkMetal');weight(side+'Thigh',begin)
 begin=len(parts);cylinder('Trouser',(s*.12,0,.66),(s*.12,0,.12),.063,'H_DarkMetal');box('Boot',(s*.12,-.07,.10),(.17,.33,.2),'H_Rubber',.03);weight(side+'Shin',begin)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();skin=bpy.context.object;skin.name='SurgeonSkin'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
skin.data.calc_loop_triangles();stats['Surgeon']={'triangles':len(skin.data.loop_triangles),'bones':len(spec)}
for v in skin.data.vertices:assert len(v.groups)==1 and abs(v.groups[0].weight-1)<.001
parts.clear();bpy.ops.object.select_all(action='DESELECT');skin.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'CHR_Surgeon.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,bake_anim=False,use_triangles=True)
skin.hide_set(True);skin.hide_render=True;rig.hide_set(True)

# A contact sheet lives with the editable source for visual review.
for i,o in enumerate(assets):
 o.hide_set(False);o.hide_render=False;o.location=((i%5)*4.5,(i//5)*4.5,0)
skin.hide_set(False);skin.hide_render=False;rig.hide_set(False);rig.location=(18,18,0)
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.world.color=(.15,.15,.15)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size in [('Key',(6,-8,18),4000,10),('Fill',(22,9,12),3000,8)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;aim(o,(8,7,0))
d=bpy.data.cameras.new('KitCamera');cam=bpy.data.objects.new('KitCamera',d);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(28,-28,27);aim(cam,(9,8,1));d.type='ORTHO';d.ortho_scale=34
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.filepath=str(QA/'Hospital_Blender_Kit.png');bpy.ops.render.render(write_still=True)
cam.location=(20,14.5,2.8);aim(cam,(18,18,1.2));d.ortho_scale=2.8
scene.render.resolution_x=900;scene.render.resolution_y=1100
scene.render.filepath=str(QA/'Surgeon_Blender.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).parent/'Hospital_AssetLibrary.blend'))
(QA/'hospital_blender_validation.json').write_text(json.dumps({'status':'PASS','assets':stats},indent=2))

# Original, deterministic mechanical cues. No downloaded sound dependencies.
audio=ROOT/'Unity/Assets/SpookTuber/Hospital/Audio';audio.mkdir(exist_ok=True)
random.seed(811)
for name,duration,pitch,rough in [('Footstep',.16,75,.6),('Door',.55,110,.45),('SurgeonWarning',1.1,180,.16),('SurgicalHum',2,53,.04),('Blade',.42,370,.3)]:
 rate=22050
 with wave.open(str(audio/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate)
  for i in range(int(rate*duration)):
   t=i/rate;env=1 if name=='SurgicalHum' else math.exp(-t*5/duration)*min(1,t*120)
   value=(math.sin(math.tau*pitch*t+(0 if name=='SurgicalHum' else 9*t*t))*.45+math.sin(math.tau*pitch*2*t)*.1+random.uniform(-rough,rough))*env
   w.writeframesraw(struct.pack('<h',int(max(-1,min(1,value*.45))*32767)))
print('SPOOKTUBER_HOSPITAL_ART_PASS '+json.dumps(stats))
import sys
sys.path.insert(0,str(Path(__file__).parent))
from build_surfaces import main as build_surfaces
build_surfaces(OUT.parent/'Textures')

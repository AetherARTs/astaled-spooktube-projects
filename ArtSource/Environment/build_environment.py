"""Original Blender environment/NPC/equipment kit for Solo revision 5.
Run: blender --background --python ArtSource/Environment/build_environment.py
The approved player meshes are not touched.
"""
import sys, math, json, random
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import asset_tools as kit
ROOT=Path(__file__).resolve().parents[2]
palette={
 'H_Plaster':(.46,.47,.40),'H_GreenPaint':(.15,.25,.22),'H_Tile':(.45,.49,.45),
 'H_Grout':(.075,.09,.085),'H_Steel':(.29,.33,.33),'H_DarkMetal':(.045,.057,.06),
 'H_Rust':(.29,.12,.055),'H_Linen':(.53,.52,.43),'H_Coat':(.5,.49,.41),
 'H_Rubber':(.019,.023,.028),'H_Glass':(.015,.04,.042),'H_Light':(.77,.84,.7),
 'H_Red':(.40,.055,.027),'H_Paper':(.72,.68,.55),'H_Amber':(.78,.47,.13),
 'P_Wood':(.29,.18,.095),'P_Fabric':(.085,.16,.19),'P_Plaster':(.61,.55,.44),
 'P_Ivory':(.77,.72,.61),'P_Brass':(.49,.32,.12),'P_Blue':(.14,.28,.34),
 'P_Leaf':(.12,.22,.12),'P_Concrete':(.26,.29,.28),'P_Screen':(.008,.012,.016)}
kit.initialize(ROOT,palette,ROOT/'Unity/Assets/SpookTuber/Environment/Models')
from asset_tools import box,cylinder,finish,export,legs,wheels,parts,assets,stats,mats,scene,OUT,QA
random.seed(451)

def cushion(name,pos,size,mat='P_Fabric',radius=.06):
 return box(name,pos,size,mat,radius)

def rail(a,b,r=.02,mat='H_DarkMetal'):return cylinder('Tube',a,b,r,mat,20)

def sphere(name,pos,size,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=pos)
 obj=bpy.context.object;obj.scale=size
 return finish(obj,name,mat)

def bolts(pos,width,height,mat='H_Steel'):
 x,y,z=pos
 for a in [-1,1]:
  for b in [-1,1]:cylinder('Fastener',(x+a*width/2,y,z+b*height/2),(x+a*width/2,y-.006,z+b*height/2),.009,mat,8)

def desk(width=1.7,depth=.75,height=.74):
 box('Timber top',(0,0,height),(width,depth,.075),'P_Wood',.025)
 for x in [-width/2+.1,width/2-.1]:
  for y in [-depth/2+.08,depth/2-.08]:rail((x,y,.03),(x,y,height-.02),.027)
 rail((-width/2+.1,depth/2-.08,.25),(width/2-.1,depth/2-.08,.25),.023)

# Apartment furniture, with separate cushions, seams, joinery and hardware.
box('Sofa base',(0,0,.29),(2.65,.96,.33),'P_Wood',.045)
for x in [-.85,0,.85]:
 cushion('Seat cushion',(x,-.04,.49),(.82,.82,.20))
 cushion('Back cushion',(x,.35,.85),(.83,.25,.66))
for x in [-1.37,1.37]:cushion('Armrest',(x,0,.62),(.23,1.05,.67))
for x in [-1.1,1.1]:
 for y in [-.34,.34]:box('Wooden foot',(x,y,.09),(.12,.12,.17),'P_Wood')
export('P_Sofa')
desk(1.4,.72,.43)
box('Lower shelf',(0,0,.16),(1.25,.58,.035),'P_Wood')
for i in range(4):box('Magazine',(-.36+i*.04,.05,.49+i*.018),(.32,.24,.015),'H_Paper')
export('P_CoffeeTable')
desk(2.1,1,.78);export('P_DiningTable')
box('Chair seat',(0,0,.47),(.49,.49,.07),'P_Wood',.035)
cushion('Seat pad',(0,-.015,.525),(.43,.43,.055),radius=.023)
for x in [-.19,.19]:
 for y in [-.19,.19]:rail((x*1.1,y*1.1,.02),(x,y,.47),.023)
 rail((x,.19,.47),(x,.22,1.03),.021)
box('Back slat',(0,.22,.9),(.47,.055,.28),'P_Wood',.035);export('P_DiningChair')
for x in [-1.3,-.44,.44,1.3]:
 box('Cabinet',(x,0,.44),(.84,.65,.88),'P_Wood',.018)
 box('Recessed door',(x,-.338,.46),(.74,.035,.70),'P_Plaster',.018)
 rail((x+.25,-.39,.49),(x+.25,-.39,.67),.012,'P_Brass')
 box('Upper cupboard',(x,.07,1.97),(.84,.43,.68),'P_Wood')
 box('Upper door',(x,-.162,1.97),(.77,.02,.61),'P_Plaster')
box('Counter',(0,0,.92),(3.55,.75,.075),'H_Grout',.025)
box('Sink rim',(-.6,-.05,.971),(.64,.47,.014),'H_Steel')
box('Sink bowl',(-.6,-.05,.98),(.51,.36,.014),'H_DarkMetal',.08)
rail((-.6,.21,.98),(-.6,.21,1.20),.015,'H_Steel');rail((-.6,.21,1.20),(-.6,.03,1.20),.015,'H_Steel')
box('Hob',(.95,-.04,.97),(.65,.52,.024),'H_Rubber')
for x in [.78,1.1]:
 for y in [-.19,.1]:cylinder('Burner',(x,y,.976),(x,y,.986),.1,'H_Steel',24)
export('P_KitchenRun')
box('Fridge body',(0,0,.91),(.76,.75,1.82),'P_Ivory',.045)
for z,h in [(1.48,.55),(.60,1.13)]:
 box('Door seal',(0,-.39,z),(.71,.02,h),'H_Rubber',.015)
 box('Fridge door',(0,-.412,z),(.70,.052,h-.025),'P_Ivory',.025)
 rail((-.26,-.475,z-.15),(-.26,-.475,z+.15),.017,'H_Steel')
export('P_Fridge')
for x in [-.53,.53]:
 for y in [-1.04,1.04]:rail((x,y,.02),(x,y,2.02),.035)
for z in [.45,1.5]:
 box('Bed deck',(0,0,z),(1.1,2.16,.085),'H_DarkMetal')
 cushion('Mattress',(0,0,z+.13),(1.02,2.04,.18),'H_Linen')
 cushion('Blanket',(0,-.36,z+.245),(1.03,1.3,.07),'P_Fabric',.03)
 cushion('Pillow',(0,.69,z+.28),(.74,.43,.14),'P_Ivory')
 for y in [-1.02,1.02]:rail((-.5,y,z+.34),(.5,y,z+.34),.022)
for z in [.2,.48,.76,1.04,1.32]:rail((.55,-.9,z),(.55,-.40,z),.02)
for y in [-.9,-.4]:rail((.55,y,.08),(.55,y,1.73),.024)
export('P_BunkBed')
for x in [-.7,.7]:box('Shelf side',(x,0,1.05),(.055,.4,2.1),'P_Wood')
for z in [.06,.53,1,1.47,2.06]:box('Shelf',(0,0,z),(1.43,.42,.035),'P_Wood')
for row in range(4):
 for i in range(11):
  z=.08+row*.47;h=random.uniform(.22,.36)
  box('Book',(-.58+i*.112,-.02,z+h/2),(.085,.29,h),['H_Paper','H_Red','P_Blue','H_Linen'][i%4],.002)
export('P_Bookshelf')
desk(2.2,.9,.77)
for x in [-.53,.53]:
 box('Monitor foot',(x,.12,.83),(.36,.28,.025),'H_DarkMetal')
 rail((x,.17,.85),(x,.17,1.18),.028)
 box('Monitor frame',(x,.17,1.25),(.91,.065,.53),'H_DarkMetal',.025)
 box('Edit display',(x,.13,1.25),(.85,.007,.47),'P_Screen',.008)
box('Keyboard',(0,-.24,.829),(.54,.18,.035),'H_Rubber',.01)
for i in range(15):box('Key column',(-.245+i*.035,-.24,.85),(.025,.143,.01),'H_Steel',.003)
box('Tower',(1,-.02,.34),(.30,.6,.62),'H_DarkMetal',.03)
for z in [.12,.25,.38]:cylinder('Fan',(1,-.325,z),(1,-.338,z),.072,'H_Steel',24)
export('P_EditDesk')
for x in [-.87,.87]:
 for y in [-.32,.32]:rail((x,y,0),(x,y,2.12),.025)
for z in [.12,.65,1.18,1.71,2.12]:box('Equipment shelf',(0,0,z),(1.82,.7,.055),'H_Steel')
for row in range(3):
 for x in [-.47,.43]:
  box('Hard case',(x,0,.37+row*.53),(.70,.51,.39),'H_DarkMetal',.035)
  box('Case seam',(x,-.265,.4+row*.53),(.66,.018,.025),'H_Steel')
  for a in [-1,1]:box('Latch',(x+a*.23,-.282,.4+row*.53),(.055,.025,.09),'P_Brass')
export('P_EquipmentRack')
for angle in range(0,360,72):
 a=math.radians(angle);rail((0,0,.15),(.33*math.cos(a),.33*math.sin(a),.08),.022)
 sphere('Caster',(.33*math.cos(a),.33*math.sin(a),.055),(.045,.045,.045),'H_Rubber')
rail((0,0,.12),(0,0,.52),.04)
cushion('Office seat',(0,0,.55),(.53,.54,.11))
cushion('Office back',(0,.24,.89),(.54,.13,.59))
for x in [-.32,.32]:rail((x,0,.54),(x,0,.77),.017);box('Arm pad',(x,0,.79),(.10,.40,.05),'H_Rubber',.02)
export('P_OfficeChair')
cylinder('Pot',(0,0,.02),(0,0,.34),.21,'P_Plaster',32)
cylinder('Soil',(0,0,.335),(0,0,.34),.193,'P_Wood',24)
for i in range(13):
 a=i*2.4;h=.65+(i%5)*.13;end=(math.cos(a)*.33,math.sin(a)*.33,h)
 rail((0,0,.30),end,.008,'P_Leaf')
 leaf=sphere('Leaf',end,(.13,.055,.25),'P_Leaf');leaf.rotation_euler=(.5*math.sin(a),.6*math.cos(a),a)
export('P_Plant')
rail((0,0,.1),(0,0,.45),.008)
bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=.30,radius2=.09,depth=.20,location=(0,0,0));finish(bpy.context.object,'Pendant shade','H_DarkMetal',.006)
sphere('Bulb',(0,0,-.09),(.06,.06,.065),'H_Light');export('P_PendantLamp')

# Hospital architecture: surface layers and hardware are authored as joined meshes.
box('Masonry',(0,0,1.8),(4,.26,3.6),'H_Plaster',.01)
for side in [-1,1]:
 box('Paint',(0,side*.14,.72),(4,.016,1.44),'H_GreenPaint',.002)
 box('Crash rail',(0,side*.18,1.04),(4,.065,.11),'H_Steel',.012)
 box('Kickboard',(0,side*.157,.13),(4,.035,.25),'H_DarkMetal',.004)
 for i in range(15):
  x=random.uniform(-1.95,1.95);z=random.uniform(.30,2.6)
  box('Peeling paint',(x,side*.15,z),(random.uniform(.02,.18),.003,random.uniform(.03,.14)),'H_Plaster',.004)
 box('Power outlet',(1.45,side*.16,.5),(.14,.055,.12),'H_Paper',.01)
 for x in [1.415,1.485]:box('Socket',(x,side*.19,.5),(.024,.004,.016),'H_Rubber',0)
export('H_Wall_Detailed')
for x in [-1.1,1.1]:box('Pocket pier',(x,0,1.8),(1.8,.36,3.6),'H_Plaster')
box('Window sill',(0,-.07,1.02),(2.5,.50,.12),'H_Steel')
box('Above window',(0,0,3.23),(2.5,.28,.74),'H_Plaster')
for x in [-1.18,0,1.18]:box('Window upright',(x,-.02,1.98),(.06,.16,1.92),'H_DarkMetal')
for z in [1.07,1.98,2.92]:box('Window crossbar',(0,-.02,z),(2.43,.16,.06),'H_DarkMetal')
box('Dirty glass',(0,0,1.99),(2.33,.03,1.81),'H_Glass',0);export('H_WindowBay')
for x in [-1.03,1.03]:box('Sliding jamb',(x,0,1.24),(.085,.43,2.48),'H_Steel')
box('Top track',(0,0,2.54),(4.35,.44,.18),'H_DarkMetal')
for y in [-.26,.26]:
 box('Pocket face',(2.08,y,1.27),(2.02,.08,2.54),'H_GreenPaint')
 for x in [1.2,2.9]:bolts((x,y,1.27),.06,2.16)
export('H_DoorPocket')
box('Sliding leaf',(0,0,1.23),(1.97,.08,2.45),'H_GreenPaint',.026)
box('Leaf steel base',(0,-.05,.39),(1.85,.02,.66),'H_Steel')
box('Window',(0,-.048,1.8),(.82,.018,.60),'H_Glass')
for x in [-.44,.44]:box('Window trim',(x,-.065,1.8),(.033,.025,.66),'H_DarkMetal')
for z in [1.47,2.13]:box('Window trim',(0,-.065,z),(.91,.025,.03),'H_DarkMetal')
for side in [-1,1]:
 rail((-.72,side*.1,1.01),(-.72,side*.1,1.33),.025,'H_Steel')
 box('Door pull recess',(-.72,side*.055,1.17),(.14,.024,.40),'H_DarkMetal')
export('H_SlidingLeaf')
box('Ceiling dark void',(0,0,.03),(4,4,.10),'H_DarkMetal',0)
for x in range(4):
 for y in range(4):
  if (x,y)==(1,2):continue
  box('Acoustic panel',(-1.5+x,-1.5+y,-.045),(.967,.967,.035),'H_Plaster',.005)
for i in range(5):
 box('T grid',(i-2,0,-.06),(.025,4,.035),'H_Steel',.001)
 box('T grid',(0,i-2,-.06),(4,.025,.035),'H_Steel',.001)
for i in range(9):box('Vent grille',(.5,-1.86+i*.07,-.071),(.68,.025,.008),'H_DarkMetal',0)
export('H_CeilingGrid')
for y,r in [(-.28,.055),(0,.035),(.20,.025)]:
 rail((-2,y,0),(2,y,0),r,'H_Rust')
 for x in [-1.7,0,1.7]:
  cylinder('Collar',(x-.025,y,0),(x+.025,y,0),r+.015,'H_Steel',20)
  rail((x,y,0),(x,y,.20),.013)
export('H_PipeRun')
for x in [-.57,.57]:box('Radiator bracket',(x,.02,.35),(.07,.08,.65),'H_Steel')
for i in range(16):cushion('Radiator fin',(-.63+i*.084,0,.42),(.068,.18,.76),'H_Paper',.018)
rail((-.75,0,.12),(.78,0,.12),.027,'H_Steel');export('H_Radiator')
desk(3.3,1.1,1.06)
box('Reception front',(0,-.48,.55),(3.2,.12,1.04),'P_Wood',.02)
for i in range(14):box('Recessed slat',(-1.5+i*.23,-.55,.55),(.012,.018,.90),'H_DarkMetal',0)
box('Paper stack',(-.7,-.1,1.16),(.28,.4,.12),'H_Paper',.003);export('H_ReceptionDesk')
for x in [-.38,.38]:
 cylinder('Large tyre',(x-.03,.18,.36),(x+.03,.18,.36),.32,'H_Rubber',32)
 cylinder('Wheel hub',(x-.034,.18,.36),(x+.034,.18,.36),.045,'H_Steel',16)
 for i in range(12):
  a=i*math.tau/12;rail((x,.18,.36),(x,.18+math.cos(a)*.29,.36+math.sin(a)*.29),.004,'H_Steel')
 rail((x,-.23,.12),(x,.17,.86),.023,'H_Steel')
 cylinder('Front caster',(x-.03,-.32,.09),(x+.03,-.32,.09),.065,'H_Rubber',16)
cushion('Seat',(0,-.04,.51),(.60,.55,.06),'H_Rubber',.02)
cushion('Back',(0,.22,.80),(.60,.05,.48),'H_Rubber',.02)
for x in [-.29,.29]:rail((x,.2,.78),(x,.39,.92),.018,'H_Steel');box('Arm rest',(x,0,.76),(.075,.5,.045),'H_Rubber')
export('H_Wheelchair')
box('Machine pedestal',(0,0,.74),(.9,.65,1.48),'H_Paper',.06)
rail((0,0,1.25),(0,0,2.40),.12,'H_Steel');rail((0,0,2.37),(.80,0,2.37),.10,'H_Steel')
box('Xray head',(.80,0,2.19),(.48,.42,.4),'H_Paper',.06)
box('Beam aperture',(.80,0,1.97),(.22,.22,.05),'H_Glass')
box('Table',(0,-.95,.86),(.8,1.8,.14),'H_Paper',.05)
box('Table base',(0,-.95,.39),(.4,.9,.78),'H_GreenPaint');export('H_Radiology')
box('Morgue bank',(0,.40,1.4),(3,.80,2.8),'H_DarkMetal')
for x in [-1,0,1]:
 for z in [.48,1.4,2.32]:
  box('Cold drawer',(x,-.03,z),(.94,.11,.85),'H_Steel',.025)
  rail((x-.17,-.12,z),(x+.17,-.12,z),.018,'H_DarkMetal')
  box('ID label',(x+.27,-.094,z+.22),(.20,.006,.075),'H_Paper',.001)
export('H_MorgueBank')
box('Sink pedestal',(0,0,.43),(.22,.29,.86),'P_Ivory',.06)
cushion('Sink basin',(0,-.1,.86),(.68,.55,.18),'P_Ivory',.065)
cushion('Basin hollow',(0,-.12,.953),(.47,.33,.008),'H_Grout',.04)
rail((0,.10,.93),(0,.10,1.12),.018,'H_Steel');rail((0,.10,1.12),(0,-.05,1.12),.018,'H_Steel')
export('H_Sink')
cushion('Toilet bowl',(0,-.15,.31),(.4,.62,.41),'P_Ivory',.14)
cushion('Seat',(0,-.22,.53),(.42,.57,.055),'H_Grout',.10)
cushion('Seat opening',(0,-.22,.56),(.28,.39,.008),'H_Rubber',.08)
box('Cistern',(0,.18,.74),(.47,.24,.47),'P_Ivory',.045)
box('Flush lever',(.17,.04,.86),(.075,.05,.035),'H_Steel');export('H_Toilet')
for i in range(11):
 x=random.uniform(-1.8,1.8);y=random.uniform(-.8,.8);h=random.uniform(.08,.35)
 ob=box('Rubble',(x,y,h/2),(random.uniform(.2,.7),random.uniform(.2,.5),h),'H_Plaster',.02);ob.rotation_euler.z=random.uniform(-1,1)
for i in range(7):
 ob=box('Broken tile',(random.uniform(-2,2),random.uniform(-.8,.8),.04),(.36,.31,.026),'H_Tile',.001);ob.rotation_euler.z=random.random()*6
export('H_Rubble')
box('Collapsed ledge',(0,0,.4),(2.3,1.3,.8),'P_Concrete',.04)
for i in range(5):rail((-1+i*.48,.54,.84),(-1+i*.48,.64,1.06),.011,'H_Rust')
export('H_MantleLedge')

# Equipment meshes: phone screen faces -Y, remapped by the scene builder.
box('Phone case',(0,0,0),(.30,.025,.48),'H_DarkMetal',.022)
box('Rubber bumper',(0,.006,0),(.318,.018,.498),'H_Rubber',.027)
box('Phone glass',(0,-.017,0),(.275,.007,.437),'P_Screen',.016)
box('Speaker grille',(0,-.022,.232),(.07,.006,.009),'H_Steel',.003)
cylinder('Camera',(.094,-.017,.233),(.094,-.023,.233),.009,'H_Glass',16)
export('EQP_Phone')
box('Glove housing',(0,0,0),(.16,.24,.09),'H_DarkMetal',.028)
box('Ceramic guard',(0,-.015,.052),(.15,.17,.035),'P_Ivory',.016)
for x in [-.07,.07]:
 rail((x,-.08,.02),(x,-.17,.05),.018,'P_Brass')
 rail((x,-.17,.05),(x*.65,-.25,.08),.014,'H_Steel')
for i in range(5):box('Vented coil',(0,-.04+i*.026,.078),(.08,.009,.014),'P_Blue',.003)
for y in [.05,.09]:box('Wrist strap',(0,y,0),(.19,.025,.1),'H_Rubber',.01)
export('EQP_GravityGlove')
rail((0,0,-.12),(0,0,.08),.027,'H_Rubber')
box('Worklight',(0,0,.18),(.29,.085,.22),'H_Amber',.025)
box('Reflector',(0,-.048,.18),(.252,.012,.18),'H_Steel',.012)
for x in [-.085,0,.085]:
 for z in [.135,.22]:box('LED',(x,-.058,z),(.055,.006,.05),'H_Light',.008)
export('EQP_ProductionLight')
box('Noisemaker',(0,0,.02),(.19,.10,.24),'H_Amber',.024)
cylinder('Speaker',(0,-.05,.025),(0,-.068,.025),.075,'H_DarkMetal',32)
for z in [-.02,.005,.03,.055,.08]:box('Grille',(0,-.073,z),(.12,.009,.007),'H_Steel',0)
box('Button',(0,-.05,.11),(.035,.027,.027),'H_Red',.005);export('EQP_Noisemaker')

def robot(name,body_color='P_Ivory',scale=1):
 groups={};root=bpy.data.objects.new(name,None);scene.collection.objects.link(root)
 def group(label,pivot):
  bpy.ops.object.select_all(action='DESELECT')
  for part in parts:part.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name+'_'+label
  scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');obj.parent=root
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
  parts.clear();groups[label]=obj
 box('Core',(0,0,1.13),(.62,.40,.68),'H_DarkMetal',.10)
 box('Chest shell',(0,-.17,1.18),(.59,.15,.56),body_color,.07)
 box('Belly seam',(0,-.265,.96),(.36,.025,.022),'H_Steel')
 for x in [-.16,.16]:
  rail((x,0,.83),(x,0,.49),.105,'H_DarkMetal')
  box('Thigh shell',(x,-.05,.65),(.23,.22,.31),body_color,.045)
  sphere('Knee',(x,-.06,.44),(.10,.10,.10),'H_DarkMetal')
  box('Shin shell',(x,-.02,.27),(.23,.27,.27),body_color,.045)
  box('Boot',(x,-.09,.09),(.28,.41,.18),body_color,.035)
  box('Sole',(x,-.09,.025),(.29,.42,.05),'H_Rubber',.025)
 box('Backpack',(0,.28,1.15),(.51,.26,.64),'H_DarkMetal',.04)
 for x in [-.23,.23]:box('Harness',(x,-.266,1.29),(.06,.03,.5),'H_Rubber',.01)
 for z in [1,1.2,1.4]:box('Pack pocket',(0,.43,z),(.4,.07,.15),'H_DarkMetal',.02)
 # Neck scarf is folded cloth, not a painted stripe.
 for i in range(4):
  band=box('Scarf fold',(0,-.12-i*.006,1.50-i*.037),(.63-i*.052,.36,.06),'H_Amber',.025);band.rotation_euler.y=-.06+i*.04
 scarf=box('Scarf tail',(.21,-.20,1.26),(.16,.09,.33),'H_Amber',.02);scarf.rotation_euler.y=-.16
 bolts((0,-.253,1.18),.43,.33);group('BobbyBody',(0,0,1))
 box('Head casing',(0,0,1.73),(.57,.41,.42),body_color,.075)
 box('Display bevel',(0,-.205,1.73),(.52,.06,.33),'H_DarkMetal',.06)
 box('Display',(0,-.24,1.73),(.46,.013,.27),'P_Screen',.055)
 for x in [-.145,.145]:
  # Glasses ring lies in the frontal XZ plane.
  bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=8,location=(x,-.27,1.735),rotation=(math.pi/2,0,0),major_radius=.107,minor_radius=.008)
  finish(bpy.context.object,'Brass spectacles','P_Brass')
  box('Amber eye',(x,-.255,1.74),(.027,.010,.103),'H_Amber',.012)
 rail((-.035,-.27,1.74),(.035,-.27,1.74),.006,'P_Brass')
 for x in [-.32,.32]:
  cylinder('Earpod',(x-.035,0,1.73),(x+.035,0,1.73),.13,'H_DarkMetal',24)
 rail((-.29,.08,1.87),(-.33,.08,2.08),.013,'H_Steel')
 box('Crown',(0,.02,1.97),(.19,.18,.10),body_color,.02);group('BobbyHead',(0,0,1.53))
 for side in [-1,1]:
  sphere('Shoulder',(side*.39,0,1.38),(.145,.15,.17),'H_DarkMetal')
  box('Upper arm',(side*.43,0,1.20),(.25,.3,.38),body_color,.07)
  sphere('Elbow',(side*.46,0,.99),(.105,.11,.11),'H_DarkMetal')
  box('Forearm',(side*.47,-.03,.83),(.23,.27,.29),body_color,.05)
  box('Palm',(side*.47,-.03,.62),(.20,.13,.15),'H_Rubber',.028)
  for i in range(4):cushion('Finger',(side*.47+(i-1.5)*.043,-.035,.51),(.036,.085,.14),'H_DarkMetal',.016)
  group('BobbyLeftArm' if side<0 else 'BobbyRightArm',(side*.38,0,1.42))
 root.scale=(scale,)*3;bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for obj in groups.values():obj.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,use_triangles=True)
 count=0
 for obj in groups.values():obj.data.calc_loop_triangles();count+=len(obj.data.loop_triangles);obj.hide_render=True;obj.hide_set(True)
 stats[name]={'triangles':count,'movable_groups':4};root.hide_set(True);return root,groups

bobby,bobby_parts=robot('NPC_Bobby')
clerk,clerk_parts=robot('NPC_SupplyClerk','P_Blue',.82)

# Editable contact sheet and a dedicated Bobby view.
for i,obj in enumerate(assets):obj.hide_set(False);obj.hide_render=False;obj.location=((i%7)*4.5,(i//7)*4.5,0)
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.world.color=(.18,.18,.18)
def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,pos,power,size in [('Key',(8,-8,18),6000,13),('Fill',(30,20,16),4000,12)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=pos;aim(obj,(12,8,0))
data=bpy.data.cameras.new('ArtCamera');camera=bpy.data.objects.new('ArtCamera',data);scene.collection.objects.link(camera);scene.camera=camera
camera.location=(40,-32,36);aim(camera,(13,10,0));data.type='ORTHO';data.ortho_scale=41
scene.render.resolution_x=1800;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
scene.render.filepath=str(QA/'v5_Environment_Blender.png');bpy.ops.render.render(write_still=True)
for obj in assets:obj.hide_render=True
for obj in bobby_parts.values():obj.hide_set(False);obj.hide_render=False
bobby.hide_set(False);camera.location=(2.8,-4,2.4);aim(camera,(0,0,1.04));data.ortho_scale=2.5
scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.filepath=str(QA/'v5_Bobby_Blender.png');bpy.ops.render.render(write_still=True)
for obj in assets:obj.hide_render=False
for obj in bobby_parts.values():obj.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).parent/'Solo_Environment_Library.blend'))
(QA/'v5_environment_art.json').write_text(json.dumps({'status':'PASS','assets':stats},indent=2))
print('SPOOKTUBER_ENVIRONMENT_ART_PASS',len(stats))

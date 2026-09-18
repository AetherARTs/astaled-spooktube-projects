"""Editable production MainCam. Blender coordinates: X right, -Y lens-forward, Z up."""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'ArtSource'))
import asset_tools as kit
palette={'H_DarkMetal':(.033,.043,.050),'H_Steel':(.25,.29,.30),'H_Rubber':(.011,.014,.018),
         'H_Glass':(.007,.025,.035),'H_Amber':(.78,.43,.10),'H_Red':(.55,.025,.015),'P_Ivory':(.64,.62,.53)}
kit.initialize(ROOT,palette,ROOT/'Unity/Assets/SpookTuber/Environment/Models')
from asset_tools import box,cylinder,finish,parts,export
for key in ('H_DarkMetal','H_Steel'):
 kit.mats[key].node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value=.72
kit.mats['H_Glass'].node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.09
def ring(name,y,r,thick,mat='H_Rubber'):
 bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=8,location=(0,y,0),major_radius=r,minor_radius=thick,rotation=(math.pi/2,0,0))
 return finish(bpy.context.object,name,mat)
def rail(name,a,b,r=.006,mat='H_Steel'):return cylinder(name,a,b,r,mat,20)
box('Magnesium chassis',(0,0,0),(.228,.245,.169),'H_DarkMetal',.028)
box('Upper removable shell',(0,-.012,.078),(.22,.215,.025),'H_Steel',.012)
box('Bottom shock rail',(0,0,-.081),(.22,.225,.023),'H_Rubber',.010)
box('Lens mount shoulder',(0,-.12,0),(.19,.028,.15),'H_DarkMetal',.025)
cylinder('Optical barrel',(0,-.115,0),(0,-.221,0),.063,'H_DarkMetal',48)
for y,r in [(-.127,.064),(-.150,.061),(-.185,.060),(-.216,.064)]:ring('Focus and zoom ring',y,r,.005)
for i in range(40):
 a=i*math.tau/40;x,z=math.sin(a)*.065,math.cos(a)*.065
 rail('Focus knurl',(x,-.151,z),(x,-.177,z),.0018,'H_Rubber')
ring('Front metal rim',-.227,.059,.003,'H_Steel')
cylinder('Recessed optical glass',(0,-.224,0),(0,-.225,0),.053,'H_Glass',48)
ring('Inner optical reflection',-.226,.041,.0013,'H_Steel')
for side in [-1,1]:
 box('Inset side plate',(side*.113,.003,.006),(.012,.172,.118),'H_Rubber',.005)
 for i in range(7):box('Heat sink slot',(side*.120,-.062+i*.014,.024),(.003,.007,.060),'H_DarkMetal',.001)
 for y in [-.083,.085]:
  for z in [-.050,.056]:rail('Case screw',(side*.119,y,z),(side*.124,y,z),.003,'H_Steel')
box('Ergonomic palm grip',(.140,.017,-.012),(.060,.176,.117),'H_Rubber',.026)
box('Grip mounting spine',(.116,.016,-.011),(.017,.180,.109),'H_Steel',.005)
for y in [-.044,-.012,.020,.052]:box('Finger groove',(.168,y,-.018),(.005,.008,.089),'H_DarkMetal',.003)
for y in [-.087,.075]:box('Strap anchor',(.175,y,0),(.025,.027,.066),'H_Steel',.005)
box('Padded hand strap',(.190,-.007,0),(.012,.161,.079),'H_Rubber',.012)
for z in [-.030,.030]:box('Strap stitch',(.197,0,z),(.001,.133,.002),'P_Ivory',.0005)
for y in [-.075,.075]:box('Carry handle arch',(0,y,.123),(.033,.036,.075),'H_Steel',.012)
box('Carry handle rubber',(0,0,.166),(.053,.196,.037),'H_Rubber',.016)
for y in [-.06,-.04,-.02,0,.02,.04,.06]:box('Handle traction',(0,y,.184),(.046,.006,.004),'H_DarkMetal',.001)
box('Hotshoe rail',(-.052,-.025,.098),(.041,.076,.011),'H_Steel',.003)
rail('Shotgun microphone',(-.068,.034,.125),(-.068,-.161,.125),.018,'H_Rubber')
for y in [-.145,-.13,-.115,-.10,-.085,-.07,-.055,-.04]:
 rail('Microphone grille',(-.087,y,.125),(-.049,y,.125),.0016,'H_DarkMetal')
for y in [-.06,.026]:box('Elastic mic cradle',(-.068,y,.105),(.050,.015,.047),'H_Steel',.005)
box('Recording button',(.068,.074,.094),(.030,.029,.015),'H_Red',.007)
for i in range(3):box('Transport key',(-.067+i*.032,.126,.057),(.022,.007,.011),'H_Rubber',.003)
box('Battery pack',(0,.128,-.047),(.159,.033,.062),'H_DarkMetal',.014)
box('Battery release latch',(-.084,.132,-.031),(.020,.026,.027),'H_Amber',.005)
box('Rear display housing',(0,.129,.012),(.198,.021,.125),'H_DarkMetal',.013)
# Glass is a separate existing Unity LCD renderer, at y=+.140 after FBX remapping.
for x in [-.094,.094]:box('LCD edge',(x,.141,.012),(.012,.013,.117),'H_Rubber',.005)
for z in [-.044,.069]:box('LCD edge',(0,.141,z),(.190,.013,.012),'H_Rubber',.005)
for i in range(4):box('Display softkey',(-.053+i*.035,.149,-.052),(.024,.006,.006),'H_Steel',.002)
box('Service label',(-.122,.045,-.025),(.002,.060,.025),'P_Ivory',.002)
for i in range(11):box('Service barcode',(-.123,.020+i*.004,-.025),(.001,.0015,.017),'H_DarkMetal',0)
for y in [-.075,-.04]:
 cylinder('I/O jack',(-.123,y,-.049),(-.129,y,-.049),.007,'H_Steel',16)
 cylinder('I/O socket',(-.13,y,-.049),(-.131,y,-.049),.004,'H_Rubber',16)
model=export('EQP_MainCam_Detailed')
model.hide_set(False);model.hide_render=False
# Retain source with a studio setup for further hand editing and review.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.world.color=(.16,.16,.16)
for location,power,size in [((-.8,-.7,1.2),180,1),((.7,.6,.6),120,.7)]:
 bpy.ops.object.light_add(type='AREA',location=location);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size;lamp.rotation_euler=(-lamp.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.48,-.65,.38));scene.camera=bpy.context.object;scene.camera.rotation_euler=(Vector((0,0,.035))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.lens=55
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Environment/EQP_MainCam_Master.blend'))
scene.render.filepath=str(ROOT/'QA/v6_MainCam_Blender.png');bpy.ops.render.render(write_still=True)
(ROOT/'QA/v6_maincam_art.json').write_text(json.dumps(kit.stats,indent=2))

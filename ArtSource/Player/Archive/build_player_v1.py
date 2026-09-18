"""SPOOKTUBER authored character, Blender 5.2; run --background --python."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Quaternion
ROOT=Path(__file__).resolve().parents[2]
PROJECT=ROOT/('My project' if (ROOT/'My project').exists() else 'Unity')
OUT=PROJECT/'Assets/SpookTuber/Characters/Player'
REFS=ROOT/'References' if (ROOT/'References').exists() else Path('D:/Projects/SPOOKYTUBER/References')
QA=ROOT/'QA'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.fps=30
groups={}
def mat(name,col,metal=0,rough=.55,emit=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    for k,v in [('Base Color',(*col,1)),('Metallic',metal),('Roughness',rough)]:
        p.inputs[k].default_value=v
    if emit:
        p.inputs['Emission Color'].default_value=(*col,1)
        p.inputs['Emission Strength'].default_value=emit
    return m
ivory=mat('MAT_Ceramic_Ivory',(.72,.66,.57),.18,.42)
edge=mat('MAT_Edge_WarmWhite',(.9,.83,.71),.08,.42)
dark=mat('MAT_Chassis_Graphite',(.028,.033,.041),.45,.44)
rubber=mat('MAT_Rubber',(.012,.015,.021),.05,.78)
red=mat('MAT_Signal_Vermilion',(.62,.032,.018),.25,.4)
steel=mat('MAT_Hardware_Steel',(.27,.30,.31),.8,.3)
glass=mat('MAT_Display_Glass',(.007,.012,.016),.15,.18)
eyes=mat('MAT_Display_Amber',(1,.54,.13),0,.28,3)
cloth=mat('MAT_Hoodie_Chalk',(.67,.64,.58),0,.92)
pants=mat('MAT_Cargo_Charcoal',(.034,.039,.05),0,.96)
webbing=mat('MAT_Webbing',(.018,.023,.031),0,.9)
def finish(o,name,m,bone=None,g='Body',smooth=False):
    o.name=name
    if m:o.data.materials.append(m)
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons:p.use_smooth=smooth
    groups.setdefault(g,[]).append(o)
    if bone:o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE')
    return o
def box(name,loc,size,m,bone=None,g='Body',bevel=.015,seg=2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.dimensions=size
    finish(o,name,m,None,g)
    if bevel:
        mod=o.modifiers.new('Edge bevel','BEVEL');mod.width=bevel;mod.segments=seg
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if bone:o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE')
    return o
def cyl(name,a,b,r,m,bone=None,g='Body',n=16,r2=None):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r if r2 is None else r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return finish(o,name,m,bone,g,True)
def orb(name,loc,size,m,bone=None,g='Body'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=loc)
    o=bpy.context.object;o.scale=size
    return finish(o,name,m,bone,g,True)
def mesh(name,vs,fs,m,bone=None,g='Body'):
    d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update()
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o)
    return finish(o,name,m,bone,g)

def panel(name,loc,size,m,bone=None,g='Head',radius=.03):
    x,y,z=loc;w,d,h=size;vs=[];n=24
    for yy in [y-d/2,y+d/2]:
        for cx,cz,start in [(w/2-radius,h/2-radius,0),(-w/2+radius,h/2-radius,90),(-w/2+radius,-h/2+radius,180),(w/2-radius,-h/2+radius,270)]:
            for k in range(6):
                t=math.radians(start+k*90/5)
                vs.append((x+cx+radius*math.cos(t),yy,z+cz+radius*math.sin(t)))
    fs=[tuple(range(n)),tuple(reversed(range(n,n*2)))]
    for i in range(n):fs.append((i,(i+1)%n,(i+1)%n+n,i+n))
    return mesh(name,vs,fs,m,bone,g)

def profile(name,rings,m,bone=None,g='Body',n=12,fold=0):
    vs=[]
    for j,(z,rx,ry,cx,cy) in enumerate(rings):
        for i in range(n):
            t=math.tau*i/n;f=1+fold*math.sin(i*2.7+j*1.9)
            vs.append((cx+rx*math.cos(t)*f,cy+ry*math.sin(t)*f,z))
    fs=[tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n
            fs.append((a,b,b+n,a+n))
    fs.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name,vs,fs,m,bone,g)
def beam(name,a,b,w,d,m,bone=None,g='Body',bevel=.008):
    a,b=Vector(a),Vector(b)
    o=box(name,(a+b)/2,(w,d,(b-a).length),m,bone,g,bevel)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o
def strap(name,pts,w,m,bone,g='Outfit'):
    for i in range(len(pts)-1):beam(name+str(i),pts[i],pts[i+1],w,.016,m,bone,g,.004)
def bolt(name,loc,bone,g='Body',r=.006):
    x,y,z=loc
    cyl(name,(x,y+.003,z),(x,y-.003,z),r,steel,bone,g,8)
    box(name+'_Slot',(x,y-.004,z),(r*1.1,.0015,.0017),dark,bone,g,0)

bpy.ops.object.armature_add(enter_editmode=True)
rig=bpy.context.object;rig.name='CHR_Player_Rig';rig.data.name='SKEL_CrewUnit'
rig.data.edit_bones.remove(rig.data.edit_bones[0])
defs={}
def bone(name,a,b,parent=None,deform=True):
    q=rig.data.edit_bones.new(name);q.head=a;q.tail=b;q.use_deform=deform
    if parent:q.parent=rig.data.edit_bones[parent]
    defs[name]=(a,b,parent)
bone('Root',(0,0,0),(0,0,.12),None,False)
for name,a,b,parent in [('Hips',.93,1.06,'Root'),('Spine',1.06,1.20,'Hips'),('Chest',1.20,1.36,'Spine'),('Neck',1.36,1.49,'Chest'),('Head',1.49,1.78,'Neck')]:
    bone(name,(0,0,a),(0,0,b),parent)
bone('Socket_HeadTop',(0,0,1.80),(0,0,1.84),'Head',False)
bone('Socket_Backpack',(0,.13,1.29),(0,.20,1.29),'Chest',False)
bone('Socket_ShoulderLight',(-.20,-.06,1.37),(-.20,-.12,1.37),'Chest',False)
for side,s in [('Left',1),('Right',-1)]:
    for name,a,b,parent in [('Shoulder',.045,.245,'Chest'),('UpperArm',.245,.49,side+'Shoulder'),('LowerArm',.49,.725,side+'UpperArm'),('Hand',.725,.827,side+'LowerArm')]:
        bone(side+name,(s*a,0,1.34),(s*b,0,1.34),parent)
    bone(side+'UpperLeg',(s*.116,0,.94),(s*.135,-.012,.54),'Hips')
    bone(side+'LowerLeg',(s*.135,-.012,.54),(s*.135,0,.17),side+'UpperLeg')
    bone(side+'Foot',(s*.135,0,.17),(s*.135,-.13,.075),side+'LowerLeg')
    bone(side+'Toes',(s*.135,-.13,.075),(s*.135,-.23,.07),side+'Foot')
    bone('Socket_'+side+'Grip',(s*.795,-.01,1.30),(s*.795,-.09,1.30),side+'Hand',False)
    labels=['Proximal','Intermediate','Distal']
    for f,y,length in [('Index',-.046,.11),('Middle',-.015,.123),('Ring',.016,.112),('Little',.044,.086)]:
        for k,(a,b) in enumerate([(0,.45),(.45,.78),(.78,1)]):
            bone(side+f+labels[k],(s*(.824+length*a),y,1.34),(s*(.824+length*b),y,1.34),side+'Hand' if k==0 else side+f+labels[k-1])
    thumb=[(s*.761,-.045,1.323),(s*.789,-.086,1.32),(s*.826,-.101,1.32),(s*.85,-.105,1.32)]
    for k,label in enumerate(labels):bone(side+'Thumb'+label,thumb[k],thumb[k+1],side+'Hand' if k==0 else side+'Thumb'+labels[k-1])
bone('Charm',(.05,-.20,1.075),(.05,-.20,.98),'Spine')
bone('Tag',(-.175,-.12,.83),(-.175,-.12,.68),'RightUpperLeg')
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True;rig.data.display_type='OCTAHEDRAL'

box('Head_Shell',(0,0,1.64),(.39,.275,.34),ivory,'Head','Head',.066,4)
box('Head_ServiceGasket',(0,.137,1.64),(.30,.018,.235),dark,'Head','Head',.033,3)
box('Head_ServiceCover',(0,.151,1.64),(.279,.016,.215),ivory,'Head','Head',.031,3)
panel('Head_FaceRim',(0,-.139,1.641),(.365,.035,.304),edge,'Head',radius=.048)
panel('Head_ScreenGasket',(0,-.162,1.641),(.302,.018,.248),rubber,'Head',radius=.040)
panel('Head_Screen',(0,-.172,1.641),(.282,.013,.229),glass,'Head',radius=.035)
for s in [-1,1]:
    panel('Eye_'+str(s),(s*.062,-.183,1.63),(.018,.007,.071),eyes,'Head','Face',radius=.008)
    for z in [1.525,1.758]:bolt('Face_Screw',(s*.148,-.161,z),'Head','Head',.004)
    for a,b,r,m in [(.183,.218,.09,rubber),(.21,.24,.074,dark),(.24,.248,.043,red),(.248,.252,.030,dark)]:
        cyl('Ear',(s*a,0,1.615),(s*b,0,1.615),r,m,'Head','Head',20)
    beam('Aerial_Lower',(s*.22,.013,1.665),(s*.246,.022,1.85),.063,.057,dark,'Head','Head',.012)
    beam('Aerial_Tip',(s*.246,.022,1.84),(s*.27,.032,2.005),.049,.041,dark,'Head','Head',.012)
    beam('Aerial_Signal',(s*.251,-.002,1.854),(s*.258,.001,1.911),.029,.007,red,'Head','Head',.003)
    cyl('Aerial_Cable',(s*.17,.046,1.79),(s*.19,.046,1.855),.013,rubber,'Head','Head',10)
    beam('Aerial_CableTop',(s*.19,.046,1.855),(s*.242,.046,1.855),.013,.013,dark,'Head','Head',.004)
    box('Head_LowerGrill',(s*.10,-.107,1.478),(.085,.05,.020),dark,'Head','Head',.006)
for x in [-.075,-.045,-.015,.015,.045,.075]:box('Rear_Vent',(x,.163,1.667),(.010,.004,.071),dark,'Head','Head',.002)
box('Rear_ID',(0,.164,1.59),(.095,.005,.028),steel,'Head','Head',.003)
for z,r,m in [(1.397,.058,dark),(1.422,.061,red),(1.449,.055,dark)]:cyl('Neck_Collar',(0,0,z-.015),(0,0,z+.015),r,m,'Neck')
profile('Torso_Core',[(1.055,.094,.065,0,0),(1.12,.115,.076,0,0),(1.28,.158,.083,0,0),(1.355,.115,.077,0,0)],dark,'Chest')
profile('Chest_Ceramic',[(1.108,.113,.076,0,-.026),(1.17,.167,.091,0,-.027),(1.30,.179,.087,0,-.014),(1.365,.12,.071,0,0)],ivory,'Chest')
box('Chest_Badge',(0,-.111,1.29),(.015,.007,.052),red,'Chest',bevel=.003)
box('Chest_Underplate',(0,-.073,1.097),(.136,.055,.087),dark,'Spine',bevel=.019)
box('Spine_Status',(0,-.106,1.09),(.059,.008,.017),red,'Spine',bevel=.004)
for z in [1.002,1.029,1.055]:box('Abdomen_Bellows',(0,0,z),(.162,.115,.014),rubber,'Spine',bevel=.006)
profile('Pelvis_Armor',[(.865,.048,.064,0,-.035),(.922,.081,.069,0,-.025),(.995,.148,.09,0,0),(1.02,.124,.077,0,0)],ivory,'Hips')
box('Back_PowerCover',(0,.09,1.235),(.194,.044,.17),ivory,'Chest',bevel=.018)
for x in [-.065,0,.065]:box('Back_Vent',(x,.119,1.25),(.025,.01,.082),dark,'Chest',bevel=.005)
for side,s in [('Left',1),('Right',-1)]:
    ua,la,hand=side+'UpperArm',side+'LowerArm',side+'Hand'
    ul,ll,foot=side+'UpperLeg',side+'LowerLeg',side+'Foot'
    orb(side+'_Shoulder',(s*.239,0,1.34),(.088,.077,.079),dark,ua)
    cyl('Shoulder_Bearing',(s*.242,-.07,1.34),(s*.242,-.082,1.34),.043,red,ua)
    beam('Bicep_Core',(s*.278,0,1.34),(s*.45,0,1.34),.074,.081,dark,ua)
    beam('Bicep_Armor',(s*.288,-.003,1.34),(s*.411,-.003,1.34),.127,.132,ivory,ua,bevel=.028)
    cyl('Elbow_Axle',(s*.479,-.067,1.34),(s*.479,.067,1.34),.05,dark,la)
    cyl('Elbow_Cap',(s*.479,-.069,1.34),(s*.479,-.075,1.34),.032,steel,la)
    beam('Forearm_Core',(s*.50,0,1.34),(s*.704,0,1.34),.065,.075,dark,la)
    beam('Forearm_Shell',(s*.532,0,1.34),(s*.680,0,1.34),.121,.14,ivory,la,bevel=.024)
    box('Forearm_Insert',(s*.582,0,1.404),(.056,.037,.009),red,la,bevel=.004)
    cyl('Wrist',(s*.697,0,1.34),(s*.739,0,1.34),.047,rubber,hand)
    box('Palm',(s*.783,0,1.34),(.098,.113,.067),dark,hand,bevel=.020)
    box('Hand_Plate',(s*.776,0,1.372),(.070,.083,.015),dark,hand,bevel=.009)
    box('Hand_Status',(s*.770,-.015,1.382),(.026,.013,.005),red,hand,bevel=.002)
    for name,(a,b,p) in defs.items():
        if name.startswith(side) and any(f in name for f in ['Index','Middle','Ring','Little','Thumb']):
            va,vb=Vector(a),Vector(b);d=vb-va
            cyl(name+'_Shell',va+d*.08,vb-d*.08,.014 if 'Thumb' not in name else .018,dark,name,n=10,r2=.012)
            orb(name+'_Joint',va,(.016,.016,.016),rubber,name)
    cyl('Hip_Core',(s*.106,0,.933),(s*.179,0,.933),.082,dark,ul)
    cyl('Hip_Ring',(s*.169,0,.933),(s*.185,0,.933),.074,red,ul)
    beam('Thigh_Core',(s*.13,0,.90),(s*.134,-.011,.59),.083,.084,dark,ul)
    profile('Thigh_Shell',[(.609,.053,.058,s*.134,-.028),(.68,.071,.068,s*.133,-.018),(.841,.066,.065,s*.126,-.012),(.899,.046,.048,s*.12,-.005)],ivory,ul)
    box('Thigh_Inset',(s*.191,.016,.764),(.028,.048,.088),dark,ul,bevel=.01)
    for a,b,r,m in [(.086,.185,.068,dark),(.184,.194,.045,red),(.194,.198,.030,dark)]:cyl('Knee',(s*a,-.012,.54),(s*b,-.012,.54),r,m,ll)
    box('Kneecap',(s*.135,-.067,.546),(.092,.043,.103),ivory,ll,bevel=.026)
    beam('Shin_Core',(s*.135,0,.48),(s*.135,0,.22),.076,.077,dark,ll)
    profile('Shin_Shell',[(.21,.047,.05,s*.135,0),(.263,.067,.061,s*.135,-.01),(.425,.065,.061,s*.135,-.01),(.479,.050,.053,s*.135,0)],ivory,ll)
    box('Shin_Inset',(s*.135,-.071,.326),(.042,.012,.092),dark,ll,bevel=.005)
    for z in [.25,.418]:bolt('Shin_Bolt',(s*.171,-.058,z),ll,r=.004)
    cyl('Ankle',(s*.086,0,.165),(s*.185,0,.165),.057,dark,foot)
    cyl('Ankle_Accent',(s*.185,0,.165),(s*.19,0,.165),.033,red,foot)
    for name,loc,size,m,b in [
        ('Outsole',(s*.135,-.076,.035),(.195,.335,.065),rubber,.023),
        ('Midsole',(s*.135,-.088,.070),(.202,.320,.060),edge,.023),
        ('Upper',(s*.135,-.057,.12),(.173,.260,.125),dark,.035),
        ('Toecap',(s*.135,-.173,.104),(.180,.126,.077),ivory,.025),
        ('Tongue',(s*.135,-.051,.185),(.104,.076,.069),red,.013),
        ('Label',(s*.135,-.093,.194),(.062,.009,.019),edge,.003)]:
        box('Shoe_'+name,loc,size,m,foot,bevel=b)
    for y,z in [(-.109,.16),(-.067,.179)]:box('Shoe_Strap',(s*.135,y,z),(.157,.032,.025),rubber,foot,bevel=.006)
    for y in [-.196,-.142,-.088,-.034,.02]:box('Tread',(s*.135,y,.006),(.151,.018,.012),dark,foot,bevel=.002)

# Garments use no simulation in Blender: their actual secondary physics is authored in Unity.
hoodie=profile('Hoodie_Torso',[(1.015,.177,.114,0,0),(1.044,.196,.13,0,0),(1.10,.184,.132,0,0),(1.16,.181,.129,0,0),(1.23,.193,.129,0,0),(1.30,.201,.126,0,0),(1.345,.187,.109,0,0),(1.385,.092,.077,0,0)],cloth,None,'Outfit',20,.035)
for bn in ['Spine','Chest']:hoodie.vertex_groups.new(name=bn)
for v in hoodie.data.vertices:
    w=max(0,min(1,(v.co.z-1.09)/.20))
    for bn,f in [('Spine',1-w),('Chest',w)]:
        if f:hoodie.vertex_groups[bn].add([v.index],f,'REPLACE')
profile('RibHem',[(.998,.171,.112,0,0),(1.028,.181,.119,0,0)],cloth,'Spine','Outfit',20)
profile('Hood',[(1.295,.134,.075,0,.11),(1.345,.167,.085,0,.115),(1.417,.131,.070,0,.095),(1.436,.094,.058,0,.077)],cloth,'Chest','Outfit',20,.02)
box('Hood_Lining',(0,.060,1.425),(.148,.116,.015),pants,'Chest','Outfit',.031,3)
for s in [-1,1]:
    strap('Hood_Lapel',[(s*.125,-.035,1.411),(s*.095,-.087,1.414),(0,-.139,1.343)],.043,cloth,'Chest')
    cyl('Cord_Eyelet',(s*.049,-.135,1.365),(s*.049,-.146,1.365),.009,steel,'Chest','Outfit',12)
    cyl('Drawcord',(s*.049,-.152,1.364),(s*.047,-.153,1.195),.0038,red,'Chest','Outfit',8)
    cyl('Cord_Aglet',(s*.047,-.153,1.204),(s*.047,-.153,1.181),.0052,red,'Chest','Outfit',8)
box('Kangaroo_Pocket',(0,-.135,1.094),(.237,.022,.088),cloth,'Spine','Outfit',.018,2)
for s in [-1,1]:beam('Pocket_Opening',(s*.113,-.153,1.078),(s*.083,-.154,1.135),.007,.004,pants,'Spine','Outfit',.002)
for side,s in [('Left',1),('Right',-1)]:
    o=profile('Hoodie_Sleeve_'+side,[(.15,.075,.079,0,0),(.21,.100,.096,0,0),(.25,.112,.106,0,0),(.28,.113,.106,0,0),(.39,.103,.095,0,0),(.48,.091,.091,0,0),(.535,.104,.093,0,0),(.62,.093,.083,0,0),(.68,.070,.062,0,0),(.698,.056,.055,0,0)],cloth,None,'Outfit',16,.045)
    for v in o.data.vertices:
        x,y,z=v.co.copy();v.co=(s*z,y,1.34+x)
    for bn in ['Chest',side+'UpperArm',side+'LowerArm']:o.vertex_groups.new(name=bn)
    for v in o.data.vertices:
        w=max(0,min(1,(abs(v.co.x)-.445)/.075))
        shoulder=max(0,min(1,(abs(v.co.x)-.18)/.13))
        for bn,f in [('Chest',1-shoulder),(side+'UpperArm',(1-w)*shoulder),(side+'LowerArm',w*shoulder)]:
            if f:o.vertex_groups[bn].add([v.index],f,'REPLACE')
    cyl('Cuff',(s*.676,0,1.34),(s*.712,0,1.34),.056,cloth,side+'LowerArm','Outfit',16)
    box('Sleeve_Tab',(s*.335,-.008,1.452),(.055,.029,.008),red,side+'UpperArm','Outfit',.003)
    tr=profile('Cargo_'+side,[(.178,.067,.071,s*.135,0),(.214,.097,.081,s*.135,0),(.275,.109,.097,s*.135,0),(.35,.094,.099,s*.135,.003),(.45,.098,.103,s*.135,-.004),(.54,.10,.109,s*.132,-.006),(.62,.106,.11,s*.128,0),(.74,.115,.111,s*.118,0),(.864,.114,.113,s*.11,0),(.975,.096,.108,s*.099,0)],pants,None,'Outfit',16,.045)
    for bn in ['Hips',side+'UpperLeg',side+'LowerLeg']:tr.vertex_groups.new(name=bn)
    for v in tr.data.vertices:
        hip=max(0,min(1,(v.co.z-.85)/.13));lo=max(0,min(1,(.60-v.co.z)/.12))
        for bn,w in [('Hips',hip),(side+'UpperLeg',(1-hip)*(1-lo)),(side+'LowerLeg',(1-hip)*lo)]:
            if w:tr.vertex_groups[bn].add([v.index],w,'REPLACE')
    box('Cargo_Pocket',(s*.194,-.068,.737),(.101,.073,.186),webbing,side+'UpperLeg','Outfit',.013)
    box('Cargo_Flap',(s*.194,-.111,.803),(.108,.025,.053),pants,side+'UpperLeg','Outfit',.008)
    box('Cargo_Reflector',(s*.195,-.128,.803),(.014,.006,.043),edge,side+'UpperLeg','Outfit',.002)
    box('Cargo_Trim',(s*.179,-.128,.803),(.006,.007,.043),red,side+'UpperLeg','Outfit',.001)
profile('Cargo_Waist',[(.935,.195,.112,0,0),(.985,.189,.112,0,0),(1.012,.173,.106,0,0)],pants,'Hips','Outfit',20)
box('Belt_Buckle',(0,-.118,.982),(.044,.015,.029),steel,'Hips','Outfit',.005)
box('Backpack',(0,.198,1.225),(.286,.157,.322),webbing,'Chest','Gear',.040,3)
box('Backpack_Pocket',(0,.286,1.17),(.249,.042,.152),pants,'Chest','Gear',.019,3)
box('Backpack_Label',(0,.311,1.186),(.055,.009,.05),ivory,'Chest','Gear',.008)
for s in [-1,1]:
    strap('Backpack_Straps',[(s*.139,.16,1.348),(s*.151,.045,1.383),(s*.143,-.09,1.346),(s*.142,-.137,1.20),(s*.155,.08,1.097)],.043,webbing,'Chest','Gear')
    box('Strap_Buckle',(s*.147,-.137,1.301),(.052,.025,.038),steel,'Chest','Gear',.006)
    box('Buckle_Inset',(s*.147,-.152,1.301),(.034,.006,.022),dark,'Chest','Gear',.003)
    box('Pack_Loop',(s*.101,.316,1.221),(.007,.01,.032),red,'Chest','Gear',.002)
strap('Crossbody_Sling',[(-.17,-.102,1.366),(-.115,-.16,1.293),(.012,-.178,1.155),(.14,-.183,1.074)],.038,webbing,'Chest','Gear')
o=box('Crossbody_Bag',(.064,-.199,1.065),(.235,.089,.14),webbing,'Spine','Gear',.018,3);o.rotation_euler[1]=-.18
box('Bag_Flap',(.064,-.253,1.087),(.215,.018,.06),pants,'Spine','Gear',.007)
for x in [-.015,.134]:box('Bag_Clasp',(x,-.27,1.081),(.012,.011,.029),red,'Spine','Gear',.003)
for z in [1.065,1.035]:cyl('Charm_Chain',(.05,-.266,z),(.05,-.266,z-.018),.0045,steel,'Charm','Gear',8)
orb('Bunny_Charm',(.05,-.271,.997),(.031,.012,.028),edge,'Charm','Gear')
for s in [-1,1]:
    orb('Bunny_Ear',(.05+s*.012,-.27,1.031),(.008,.011,.024),edge,'Charm','Gear')
    orb('Bunny_Eye',(.05+s*.010,-.283,1.0),(.003,.002,.004),dark,'Charm','Gear')
box('Crew_Tag',(-.21,-.148,.73),(.047,.013,.123),red,'Tag','Gear',.005)
for z in [.718,.73]:box('Tag_Print',(-.21,-.158,z),(.027,.003,.004),edge,'Tag','Gear',.001)
bolt('Tag_Rivet',(-.21,-.158,.778),'Tag','Gear',.004)

# Hidden under clothing at runtime, kept in the base character for wardrobe changes.
groups['Chassis']=[]
exposed=[]
covered={'Chest','Spine','Hips','LeftUpperArm','RightUpperArm','LeftLowerArm','RightLowerArm','LeftUpperLeg','RightUpperLeg','LeftLowerLeg','RightLowerLeg'}
for o in groups['Body']:
    (groups['Chassis'] if any(vg.name in covered for vg in o.vertex_groups) else exposed).append(o)
groups['Body']=exposed
objects={}
for group,parts in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
    o=bpy.context.object;o.name='CHR_Player_'+group
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # Scale the actual authored vertices and rest bones, leaving all object transforms identity.
    for v in o.data.vertices:v.co*=.9
    o.parent=rig
    mod=o.modifiers.new('Crew skeleton','ARMATURE');mod.object=rig
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.008)
    bpy.ops.object.mode_set(mode='OBJECT')
    objects[group]=o;o['asset_role']=group
face=objects['Face']
face.shape_key_add(name='Basis')
blink=face.shape_key_add(name='Blink')
for v in blink.data:v.co.z=(v.co.z-1.63*.9)*.08+1.63*.9
bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:b.head*=.9;b.tail*=.9
bpy.ops.object.mode_set(mode='OBJECT')

def clip(name,frames,kind):
    rig.animation_data_create();rig.animation_data.action=None
    for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
    for frame in range(1,frames+1,2):
        t=(frame-1)/(frames-1)*math.tau
        for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
        for side,s in [('Left',1),('Right',-1)]:
            p=rig.pose.bones[side+'UpperArm']
            basis=p.bone.matrix_local.to_3x3().to_quaternion()
            down=Quaternion((0,1,0),s*math.radians(78))
            swing=Quaternion((1,0,0),-math.sin(t)*s*.35 if kind!='idle' else 0)
            p.rotation_euler=(basis.inverted() @ swing @ down @ basis).to_euler()
            if kind!='idle':
                stride=math.sin(t)*s
                rig.pose.bones[side+'UpperLeg'].rotation_euler[0]=stride*(.48 if kind=='walk' else .7)
                rig.pose.bones[side+'LowerLeg'].rotation_euler[0]=max(0,-stride)*(.65 if kind=='walk' else 1.1)
                rig.pose.bones[side+'Foot'].rotation_euler[0]=-stride*.12

        rig.pose.bones['Hips'].location.z=.008*math.sin(t) if kind=='idle' else .015*math.cos(t*2)
        rig.pose.bones['Chest'].rotation_euler[0]=.015*math.sin(t)
        rig.pose.bones['Head'].rotation_euler[1]=.02*math.sin(t)
        for p in rig.pose.bones:
            p.keyframe_insert('rotation_euler',frame=frame,group=p.name)
            if p.name=='Hips':p.keyframe_insert('location',frame=frame,group=p.name)
    a=rig.animation_data.action;a.name=name;a.use_fake_user=True
    return a
actions=[clip('Idle',61,'idle'),clip('Walk',31,'walk'),clip('Run',21,'run')]
rig.animation_data.action=None
for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
scene.frame_set(1)
report={'blender':bpy.app.version_string,'bones':len(rig.data.bones),'meshes':{},'clips':[a.name for a in actions]}
for g,o in objects.items():
    o.data.calc_loop_triangles()
    assert o.data.uv_layers,g+' no UV'
    for v in o.data.vertices:
        assert v.groups,g+' unweighted'
        assert abs(sum(q.weight for q in v.groups)-1)<1e-4,g+' weight sum'
        assert len(v.groups)<=4,g+' exceeds 4 influences'
    report['meshes'][g]={'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'materials':len(o.data.materials)}
report['status']='PASS'
(QA/'blender_validation.json').write_text(json.dumps(report,indent=2))
def export(name,include):
    face.data.shape_keys.key_blocks['Blink'].value=0
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for g in include:objects[g].select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,use_triangles=True)
export('CHR_Player_Base',['Body','Chassis','Head','Face'])
export('CHR_Player_Hoodie',['Body','Chassis','Head','Face','Outfit','Gear'])
refcol=bpy.data.collections.new('Reference sheets');scene.collection.children.link(refcol)
for i,name in enumerate(['Robot01.png','Robot02.png','SPOOKTUBER Character Sheets.png','SPOOKTUBER Head and Cosmetis Sheets.png']):
    im=bpy.data.images.load(str(REFS/name));im.pack()
    o=bpy.data.objects.new('REF_'+name,None);refcol.objects.link(o);o.empty_display_type='IMAGE';o.data=im
    o.empty_display_size=2;o.location=(3+i*2.1,.6,.9);o.rotation_euler=(math.pi/2,0,0);o.hide_render=True
refcol.hide_viewport=True
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.065,.085,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
box('Studio_Floor',(0,0,-.042),(200,200,.07),mat('Studio',(.047,.060,.079)),None,'Studio',0)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size,col in [('Key',(-3,-4,5),500,4,(1,.87,.72)),('Fill',(3,-1,3),260,3,(.71,.83,1)),('Rim',(0,3,4),650,3,(.8,.88,1))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;aim(o,(0,0,1))
d=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',d);scene.collection.objects.link(cam)
scene.camera=cam;d.type='ORTHO';d.ortho_scale=2.16
rig.animation_data.action=actions[0];scene.frame_set(1)
def render(name,loc,outfit):
    face.data.shape_keys.key_blocks['Blink'].value=0
    for g in ['Outfit','Gear']:objects[g].hide_render=not outfit;objects[g].hide_set(not outfit)
    objects['Chassis'].hide_render=outfit;objects['Chassis'].hide_set(outfit)
    cam.location=loc;aim(cam,(0,0,.92))
    scene.render.filepath=str(QA/(name+'.png'));bpy.ops.render.render(write_still=True)
render('Player_Base_ThreeQuarter',(2.7,-5,2.3),False)
render('Player_Base_Front',(0,-5,1.15),False)
render('Player_Hoodie_ThreeQuarter',(2.7,-5,2.3),True)
render('Player_Hoodie_Back',(-2.8,5,2),True)
cam.location=(2.7,-5,2.3);aim(cam,(0,0,.92))
rig.animation_data.action=None
for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
face.data.shape_keys.key_blocks['Blink'].value=0
# Open the editable source in an uncluttered material-colored review view.
rig.animation_data.action=actions[0];scene.frame_set(1)
rig.hide_set(True)
for o in scene.objects:
    if o.type in {'CAMERA','LIGHT'} or o.name=='Studio_Floor':o.hide_set(True)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        space=area.spaces.active
        space.region_3d.view_distance=2.8
        space.region_3d.view_location=(0,0,.91)
        space.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
        space.region_3d.view_perspective='ORTHO'
        space.shading.type='SOLID';space.shading.color_type='MATERIAL'
        space.shading.show_cavity=True

bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).parent/'CHR_Player_Master.blend'))
print('SPOOKTUBER_MODEL_PASS '+json.dumps(report))


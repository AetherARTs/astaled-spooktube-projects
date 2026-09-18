import bpy, math, json, random, wave, struct
from pathlib import Path
from mathutils import Vector

def initialize(root, palette, output):
 global ROOT,OUT,QA,scene,mats,parts,assets,stats
 ROOT=Path(root);OUT=Path(output);QA=ROOT/'QA'
 OUT.mkdir(parents=True,exist_ok=True);QA.mkdir(exist_ok=True)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 scene=bpy.context.scene;scene.unit_settings.system='METRIC'
 mats={};parts=[];assets=[];stats={}
 for name,color in palette.items():
  m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
  bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
  bs.inputs['Roughness'].default_value=.65
  bs.inputs['Metallic'].default_value=.7 if 'Metal' in name or name=='H_Steel' else 0
  mats[name]=m

def finish(o,name,mat,bevel=0):
 o.name=name;o.data.materials.append(mats[mat])
 bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3
  bpy.ops.object.modifier_apply(modifier=mod.name)
 for face in o.data.polygons:face.use_smooth=True
 mod=o.modifiers.new('Weighted corners','WEIGHTED_NORMAL');mod.keep_sharp=True
 bpy.ops.object.modifier_apply(modifier=mod.name)
 parts.append(o);return o
def box(name,pos,size,mat='H_Steel',bevel=.01):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.dimensions=size
 return finish(o,name,mat,bevel)
def cylinder(name,a,b,r,mat='H_Steel',vertices=12):
 a,b=Vector(a),Vector(b);d=b-a
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
 return finish(o,name,mat,.003)
def export(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 uv=o.data.uv_layers.active or o.data.uv_layers.new(name='Surface metres')
 for face in o.data.polygons:
  axis=max(range(3),key=lambda k:abs(face.normal[k]));axes=[k for k in range(3) if k!=axis]
  for loop in face.loop_indices:
   point=o.data.vertices[o.data.loops[loop].vertex_index].co
   uv.data[loop].uv=(point[axes[0]]/2,point[axes[1]]/2)
 o.data.calc_loop_triangles();stats[name]={'triangles':len(o.data.loop_triangles),'vertices':len(o.data.vertices)}
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,use_triangles=True)
 assets.append(o);parts.clear();o.hide_set(True);o.hide_render=True
 return o
def legs(width,length,height,rad=.025):
 for x in [-width/2,width/2]:
  for y in [-length/2,length/2]:cylinder('Leg',(x,y,.05),(x,y,height),rad)
def wheels(width,length,z=.09):
 for x in [-width/2,width/2]:
  for y in [-length/2,length/2]:
   cylinder('Caster',(x,y,z),(x,y,z+.12),.022)
   cylinder('Tyre',(x-.035,y,z),(x+.035,y,z),.065,'H_Rubber')


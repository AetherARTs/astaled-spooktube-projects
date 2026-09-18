"""Original 1K tileable albedo/normal/smoothness maps; no external assets."""
import bpy, numpy as np
from pathlib import Path
OUT=Path(__file__).resolve().parents[2]/'Unity/Assets/SpookTuber/Environment/Textures'
OUT.mkdir(parents=True,exist_ok=True)
N=1024;rng=np.random.default_rng(4501)
x,y=np.meshgrid(np.arange(N,dtype=np.float32)/N,np.arange(N,dtype=np.float32)/N)
def noise(gx,gy=None):
 gy=gy or gx;grid=rng.random((gy+1,gx+1));grid[-1,:]=grid[0,:];grid[:,-1]=grid[:,0]
 xx=x*gx;yy=y*gy;ix=xx.astype(int);iy=yy.astype(int);u=xx-ix;v=yy-iy;u=u*u*(3-2*u);v=v*v*(3-2*v)
 return (grid[iy,ix]*(1-u)+grid[iy,ix+1]*u)*(1-v)+(grid[iy+1,ix]*(1-u)+grid[iy+1,ix+1]*u)*v
def save(name,data):
 data=np.clip(data,0,1).astype(np.float32)
 if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
 if data.shape[2]==3:data=np.concatenate([data,np.ones((N,N,1),dtype=np.float32)],axis=2)
 image=bpy.data.images.new(name,width=N,height=N,alpha=True);image.pixels.foreach_set(data.ravel());image.filepath_raw=str(OUT/(name+'.png'));image.file_format='PNG';image.save();bpy.data.images.remove(image)
for kind in ['Plaster','CleanPlaster','Concrete','Wood','Fabric','Metal','Tile']:
 broad=noise(4)*.5+noise(13)*.3+noise(43)*.2;grain=noise(140)
 h=broad*.15+grain*.025;albedo=.78+broad*.17+grain*.035;rough=.74
 if kind=='Plaster':
  chips=np.maximum(0,noise(24)-.75)*2;h-=chips*.04;albedo-=chips*.09
 elif kind=='CleanPlaster':
  h=grain*.004;albedo=.93+broad*.035+grain*.012
 elif kind=='Concrete':
  pores=(rng.random((N,N))>.993)*.15;h-=pores;albedo-=pores*.9;albedo*=.85
 elif kind=='Wood':
  grainwood=np.sin(y*2*np.pi*70+noise(6,28)*8+np.sin(x*2*np.pi)*4)
  h+=grainwood*.012;albedo=.70+grainwood*.035+broad*.17;rough=.48
 elif kind=='Fabric':
  weave=(np.sin(x*2*np.pi*170)+np.sin(y*2*np.pi*170))*.5;h+=weave*.018;albedo=.82+broad*.1+weave*.04;rough=.9
 elif kind=='Metal':
  corrosion=np.maximum(0,broad-.58)*3;h+=corrosion*.1;albedo=.83+grain*.07-corrosion*.29;rough=.43
 elif kind=='Tile':
  h=grain*.0015+broad*.006;albedo=.89+broad*.08;rough=.34
 dy,dx=np.gradient(h);normal=np.dstack((-dx*16,-dy*16,np.ones_like(h)));normal/=np.linalg.norm(normal,axis=2)[:,:,None]
 save('TEX_'+kind+'_Albedo',albedo)
 save('TEX_'+kind+'_Normal',normal*.5+.5)
 mask=np.zeros((N,N,4),dtype=np.float32);mask[:,:,0]=.72 if kind=='Metal' else 0;mask[:,:,3]=np.clip(1-rough+(broad-.5)*.2,0,1)
 save('TEX_'+kind+'_Mask',mask)
print('SPOOKTUBER_MATERIALS_PASS / 21 original 1024 maps')

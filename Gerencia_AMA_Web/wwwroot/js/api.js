const tokenKey = 'ama.accessToken';
export const token = () => localStorage.getItem(tokenKey);
export function clearSession(){ localStorage.removeItem(tokenKey); localStorage.removeItem('ama.user'); }
export const currentUser = () => { try{return JSON.parse(localStorage.getItem('ama.user')||'null')}catch{return null} };

export async function request(path, options={}) {
  const headers = new Headers(options.headers || {});
  if(token()) headers.set('Authorization', `Bearer ${token()}`);
  if(options.body && !(options.body instanceof FormData)) headers.set('Content-Type','application/json');
  const response = await fetch(path,{...options,headers});
  if(response.status===401){clearSession();location.reload();throw new Error('Sua sessão expirou. Entre novamente.');}
  if(!response.ok){const body=await response.text();let message=body;try{const data=JSON.parse(body);message=data.erro||data.title||data.message||body}catch{}throw new Error(message||`Falha (${response.status})`)}
  return response.status===204?null:response.json();
}
export async function login(email,senha){const data=await request('/api/Auth/login',{method:'POST',body:JSON.stringify({email,senha})});localStorage.setItem(tokenKey,data.accessToken);localStorage.setItem('ama.user',JSON.stringify(data.usuario));return data.usuario;}
export const api={me:()=>request('/api/Auth/me'),updateMe:data=>request('/api/Auth/me',{method:'PUT',body:JSON.stringify(data)}),users:()=>request('/api/Usuarios'),user:id=>request(`/api/Usuarios/${id}`),createUser:data=>request('/api/Usuarios',{method:'POST',body:JSON.stringify(data)}),updateUser:(id,data)=>request(`/api/Usuarios/${id}`,{method:'PUT',body:JSON.stringify(data)}),removeUser:id=>request(`/api/Usuarios/${id}`,{method:'DELETE'}),types:()=>request('/api/TiposDoacao'),createType:data=>request('/api/TiposDoacao',{method:'POST',body:JSON.stringify(data)}),updateType:(id,data)=>request(`/api/TiposDoacao/${id}`,{method:'PUT',body:JSON.stringify(data)}),deleteType:id=>request(`/api/TiposDoacao/${id}`,{method:'DELETE'}),debits:()=>request('/api/DebitosMembros'),myDebits:()=>request('/api/DebitosMembros/me'),createDebit:data=>request('/api/DebitosMembros',{method:'POST',body:JSON.stringify(data)}),createMyDebit:data=>request('/api/DebitosMembros/meus-debitos',{method:'POST',body:JSON.stringify(data)}),approve:id=>request(`/api/DebitosMembros/${id}/aprovar`,{method:'PATCH'}),reject:id=>request(`/api/DebitosMembros/${id}/rejeitar`,{method:'PATCH'}),uploadProof:(id,file,treasurer=true)=>{const body=new FormData();body.append('Arquivo',file);const path=treasurer?`/api/DebitosMembros/${id}/anexar-comprovante`:`/api/DebitosMembros/meus-debitos/${id}/anexar-comprovante`;return request(path,{method:'POST',body})}};

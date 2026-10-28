import {api,login,currentUser,clearSession} from './api.js';
import {root,escapeHtml,toast,modal,activeInMonth} from './ui.js';
import {renderCalendar} from './calendar.js';
import {renderDonations} from './donations.js';
import {renderUsers} from './users.js';
import {renderTypes,editType} from './types.js';
import {renderSettings} from './settings.js';

const state={view:'calendar',me:null,isTreasurer:false,users:[],types:[],debits:[],month:`${new Date().getFullYear()}-${String(new Date().getMonth()+1).padStart(2,'0')}`};
window.amaState=state;
const admin=user=>String(user?.perfil)==='2'||String(user?.perfil).toLowerCase()==='tesoureiro';

function loginScreen(){
  root.innerHTML=`<main class="login-wrap"><form class="login-card" id="login"><div class="brand"><div class="brand-mark">A</div><div><strong>AMA</strong><small>GESTÃO DE DOAÇÕES</small></div></div><h1>Bem-vindo de volta</h1><p>Entre para acompanhar suas contribuições${state.me&&admin(state.me)?' e acessar a tesouraria':''}.</p><div class="form-field"><label>E-mail</label><input name="email" type="email" required autocomplete="username" placeholder="seu@email.com"></div><div class="form-field"><label>Senha</label><input name="senha" type="password" required autocomplete="current-password" placeholder="Sua senha"></div><button class="btn">Entrar</button></form></main>`;
  root.querySelector('#login').onsubmit=async e=>{e.preventDefault();const form=new FormData(e.currentTarget);try{await login(form.get('email'),form.get('senha'));await render()}catch(err){toast(err.message)}};
}

async function load(){
  state.me=await api.me();state.isTreasurer=admin(state.me);localStorage.setItem('ama.user',JSON.stringify(state.me));
  state.types=await api.types();
  if(state.isTreasurer){[state.users,state.debits]=await Promise.all([api.users(),api.debits()]);}
  else {state.users=[];state.debits=await api.myDebits();}
}

async function render(){
  if(!currentUser()){loginScreen();return;}
  try{await load()}catch(err){toast(err.message);if(!localStorage.getItem('ama.accessToken'))loginScreen();return;}
  const user=state.me;
  const nav=state.isTreasurer?`<div><div class="nav-label">TESOURARIA</div><nav class="nav"><button data-view="calendar"><span class="icon">▦</span>Painel geral</button><button data-view="donations"><span class="icon">♧</span>Doações</button><button data-view="types"><span class="icon">✳</span>Campanhas</button><button data-view="users"><span class="icon">♙</span>Membros</button></nav></div>`:`<div><div class="nav-label">MINHA CONTA</div><nav class="nav"><button data-view="calendar"><span class="icon">▦</span>Meu calendário</button><button data-view="donations"><span class="icon">♧</span>Minhas doações</button></nav></div>`;
  root.innerHTML=`<div class="layout"><aside class="sidebar" id="sidebar"><div class="brand"><div class="brand-mark">A</div><div><strong>AMA</strong><small>GESTÃO DE DOAÇÕES</small></div></div>${nav}<div><div class="nav-label">CONTA</div><nav class="nav"><button data-view="settings"><span class="icon">⚙</span>Configurações</button></nav></div><div class="side-bottom"><div class="profile-menu"><button class="profile account-trigger" id="accountMenuToggle" aria-expanded="false" aria-controls="accountPopover"><div class="avatar">${escapeHtml((user.nome||'A').slice(0,1).toUpperCase())}</div><span class="account-name"><strong>${escapeHtml(user.nome||'Usuário')}</strong><small>${state.isTreasurer?'Tesoureiro':'Membro'}</small></span><span class="account-chevron">⌃</span></button><div class="account-popover" id="accountPopover" hidden><div class="account-popover-head"><strong>${escapeHtml(user.nome||'Usuário')}</strong><small>${escapeHtml(user.email||'')}</small></div><button id="accountSettings"><span>⚙</span>Configurações da conta</button><button id="logoutMenu" class="logout-item"><span>↪</span>Sair da conta</button></div></div></div></aside><main class="main"><header class="topbar"><div style="display:flex;align-items:flex-start;gap:11px"><button class="menu-toggle" id="menu">☰</button><div><div class="eyebrow">AMA · ${state.isTreasurer?'TESOURARIA':'ÁREA DO MEMBRO'}</div><h1 class="heading" id="pageTitle"></h1><p class="subheading" id="pageSub"></p></div></div><div class="top-actions"><div class="period" id="periodControl"><button id="prevMonth" aria-label="Mês anterior">‹</button><input id="monthPicker" type="month" value="${state.month}" aria-label="Mês de referência"><button id="nextMonth" aria-label="Próximo mês">›</button></div><button class="btn" id="newAction"></button></div></header><section id="page"></section></main></div>`;
  const accountToggle=root.querySelector('#accountMenuToggle'),accountPopover=root.querySelector('#accountPopover');
  accountToggle.onclick=()=>{accountPopover.hidden=!accountPopover.hidden;accountToggle.setAttribute('aria-expanded',String(!accountPopover.hidden))};
  root.querySelector('#accountSettings').onclick=()=>{accountPopover.hidden=true;go('settings')};
  root.querySelector('#logoutMenu').onclick=logout;
  root.querySelector('#menu').onclick=()=>root.querySelector('#sidebar').classList.toggle('open');
  root.querySelectorAll('[data-view]').forEach(button=>button.onclick=()=>go(button.dataset.view));
  root.querySelector('#monthPicker').onchange=e=>{state.month=e.target.value;go(state.view)};
  root.querySelector('#prevMonth').onclick=()=>shiftMonth(-1);root.querySelector('#nextMonth').onclick=()=>shiftMonth(1);
  await go(state.view);
}

function logout(){clearSession();state.me=null;state.debits=[];state.users=[];state.types=[];loginScreen();}
function shiftMonth(amount){const[y,m]=state.month.split('-').map(Number),date=new Date(y,m-1+amount,1);state.month=`${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}`;root.querySelector('#monthPicker').value=state.month;go(state.view)}

async function go(view){
  state.view=view;root.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',b.dataset.view===view));root.querySelector('#sidebar').classList.remove('open');
  const accountPopover=root.querySelector('#accountPopover'),accountToggle=root.querySelector('#accountMenuToggle');if(accountPopover){accountPopover.hidden=true;accountToggle?.setAttribute('aria-expanded','false')}
  const titles={calendar:[state.isTreasurer?'Painel da tesouraria':'Meu calendário','Resumo mensal, campanhas e atalhos para as áreas administrativas.'],donations:[state.isTreasurer?'Doações':'Minhas doações','Filtre por mês e situação; abra um lançamento para conferir o comprovante.'],types:['Campanhas de doação','Gerencie valores, períodos e membros destinatários.'],users:['Membros','Gerencie cadastros, histórico e perfis de acesso.'],settings:['Configurações da conta','Atualize seus dados pessoais e preferências de acesso.']};
  const [title,subtitle]=titles[view];root.querySelector('#pageTitle').textContent=title;root.querySelector('#pageSub').textContent=subtitle;
  const button=root.querySelector('#newAction'),period=root.querySelector('#periodControl');
  period.classList.toggle('hide',view==='settings'||view==='types'||view==='users');
  button.classList.toggle('hide',view==='settings'||(view==='donations'&&!state.isTreasurer));
  button.textContent=view==='users'?'＋ Novo membro':view==='types'?'＋ Nova campanha':state.isTreasurer?'＋ Novo débito':'Enviar comprovante';
  button.onclick=view==='users'?()=>openUser():view==='types'?()=>editType(null,state,refresh):state.isTreasurer?()=>openNewDebit():()=>go('donations');
  const target=root.querySelector('#page');target.innerHTML='<div class="empty">Carregando…</div>';
  try{if(view==='calendar')await renderCalendar(target,state,refresh);else if(view==='donations')await renderDonations(target,state,refresh);else if(view==='types')renderTypes(target,state,refresh);else if(view==='users')await renderUsers(target,state,refresh);else renderSettings(target,state,refresh,logout)}catch(err){target.innerHTML=`<div class="panel empty">${escapeHtml(err.message)}</div>`}
}
async function refresh(){await load();await go(state.view)}

function openNewDebit(){
  const isAdmin=state.isTreasurer,month=state.month;
  const body=`<form id="debitForm"><div class="form-grid"><div class="form-field"><label>Mês de referência</label><input name="month" type="month" value="${month}" required></div>${isAdmin?`<div class="form-field"><label>Membro</label><select name="user" required><option value="">Escolha um usuário</option>${state.users.map(u=>`<option value="${u.id}">${escapeHtml(u.nome)}${String(u.perfil)==='2'?' · Tesoureiro':''}</option>`).join('')}</select></div>`:''}<div class="form-field full"><label>Tipo de doação</label><select name="type" required><option value="">Escolha o tipo</option></select><small style="color:#75858c">Tipos fora do período, não destinados ao usuário ou já lançados não aparecem.</small></div><div class="form-field full"><label>Valor</label><input name="amount" type="number" min="0.01" step="0.01" placeholder="0,00" required></div></div></form>`;
  const m=modal('Registrar doação','Um lançamento por tipo, membro e mês.',body,'<button class="btn ghost" data-cancel>Cancelar</button><button class="btn" form="debitForm">Criar doação</button>');
  m.querySelector('[data-cancel]').onclick=()=>m.remove();
  const form=m.querySelector('#debitForm'),month=form.elements.month,type=form.elements.type,user=form.elements.user;
  const populate=()=>{const period=month.value,memberId=isAdmin?Number(user.value):state.me.id;type.innerHTML='<option value="">Escolha o tipo</option>'+state.types.filter(t=>activeInMonth(t,period)&&(t.todosUsuarios||(t.usuariosIds||[]).includes(memberId))&&!state.debits.some(d=>d.usuarioId===memberId&&d.tipoDoacaoId===t.id&&String(d.mesReferencia||d.dataCriacao).slice(0,7)===period)).map(t=>`<option value="${t.id}" data-amount="${Number(t.valorPadrao)||0}">${escapeHtml(t.nome)} · ${new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(t.valorPadrao||0)}</option>`).join('')};
  type.onchange=()=>{const option=type.selectedOptions[0];form.elements.amount.value=option?.dataset.amount||''};
  month.onchange=populate;user?.addEventListener('change',populate);populate();
  form.onsubmit=async e=>{e.preventDefault();const data=new FormData(form),period=data.get('month'),typeId=Number(data.get('type')),memberId=isAdmin?Number(data.get('user')):state.me.id;if(state.debits.some(d=>d.usuarioId===memberId&&d.tipoDoacaoId===typeId&&String(d.mesReferencia||d.dataCriacao).slice(0,7)===period)){toast('Já existe uma doação deste tipo para este membro no mês selecionado.');return}const payload={tipoDoacaoId:typeId,valor:Number(data.get('amount')),mesReferencia:`${period}-01`};try{if(isAdmin)await api.createDebit({usuarioId:memberId,...payload});else await api.createMyDebit(payload);m.remove();toast('Doação registrada.');await refresh()}catch(err){toast(err.message)}};
}

function openUser(user=null){
  const isEdit=!!user,profile=String(user?.perfil)==='2'?'2':'1';
  const body=`<form id="userForm"><div class="form-grid"><div class="form-field full"><label>Nome completo</label><input name="name" minlength="3" required value="${escapeHtml(user?.nome||'')}"></div><div class="form-field full"><label>E-mail</label><input name="email" type="email" required value="${escapeHtml(user?.email||'')}"></div><div class="form-field full"><label>${isEdit?'Nova senha (opcional)':'Senha inicial'}</label><input name="password" type="password" ${isEdit?'':'required'} minlength="6" placeholder="${isEdit?'Deixe vazia para manter a senha atual':'Mínimo de 6 caracteres'}"></div><div class="form-field full"><label>Perfil de acesso</label><select name="role"><option value="1" ${profile==='1'?'selected':''}>Membro</option><option value="2" ${profile==='2'?'selected':''}>Tesoureiro</option></select></div></div></form>`;
  const m=modal(isEdit?'Editar membro':'Cadastrar membro','Nome, e-mail, senha e perfil de acesso.',body,'<button class="btn ghost" data-cancel>Cancelar</button><button class="btn" form="userForm">Salvar membro</button>');m.querySelector('[data-cancel]').onclick=()=>m.remove();
  m.querySelector('#userForm').onsubmit=async e=>{e.preventDefault();const f=new FormData(e.currentTarget),payload={nome:f.get('name'),email:f.get('email'),novaSenha:f.get('password')||null,perfil:Number(f.get('role'))};try{if(isEdit)await api.updateUser(user.id,payload);else await api.createUser({nome:payload.nome,email:payload.email,senha:payload.novaSenha,perfil:payload.perfil});m.remove();toast(isEdit?'Cadastro atualizado.':'Membro cadastrado.');await refresh()}catch(err){toast(err.message)}};
}

window.amaActions={openUser,openNewDebit,refresh};window.amaGo=go;render();

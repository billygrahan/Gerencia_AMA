import {api} from './api.js';
import {escapeHtml,toast,modal,monthLabel,money,badge} from './ui.js';

const roleLabel=user=>String(user.perfil)==='2'||String(user.perfil).toLowerCase()==='tesoureiro'?'Tesoureiro':'Membro';

export async function renderUsers(target,state,refresh){
  let search='',role='all';
  target.innerHTML=`<section class="panel"><div class="panel-head"><div><h2 class="panel-title">Membros e acessos</h2><div class="panel-note">Consulte o histórico, atualize dados e gerencie os perfis da tesouraria.</div></div><span class="legend">${state.users.length} pessoas</span></div><div class="toolbar" style="margin-bottom:14px"><input class="field search" id="userSearch" placeholder="Buscar por nome ou e-mail…"><select class="select" id="roleFilter"><option value="all">Todos os perfis</option><option value="1">Membros</option><option value="2">Tesoureiros</option></select></div><div class="table-wrap"><table><thead><tr><th>Usuário</th><th>Perfil</th><th>Doações</th><th>Em aberto</th><th></th></tr></thead><tbody id="userRows"></tbody></table></div><div class="mobile-cards" id="userCards"></div></section>`;
  const paint=()=>{
    const q=search.toLocaleLowerCase('pt-BR');
    const filtered=state.users.filter(user=>(!q||`${user.nome} ${user.email}`.toLocaleLowerCase('pt-BR').includes(q))&&(role==='all'||String(user.perfil)===role));
    const row=user=>{
      const debits=state.debits.filter(debit=>debit.usuarioId===user.id),open=debits.filter(debit=>!isPaid(debit)&&!isRejected(debit)).length;
      const badgeClass=roleLabel(user)==='Tesoureiro'?'paid':'closed';
      return `<tr><td><div class="person"><div class="avatar">${escapeHtml((user.nome||'U').slice(0,1).toUpperCase())}</div><div><strong>${escapeHtml(user.nome)}</strong><small>${escapeHtml(user.email)}</small></div></div></td><td><span class="badge ${badgeClass}">${roleLabel(user)}</span></td><td>${debits.length}</td><td>${open}</td><td><div class="actions"><button class="btn secondary" data-history="${user.id}">Histórico</button><button class="btn secondary" data-edit="${user.id}">Editar</button><button class="icon-btn" data-remove="${user.id}" aria-label="Excluir usuário">×</button></div></td></tr>`;
    };
    const card=user=>{
      const debits=state.debits.filter(debit=>debit.usuarioId===user.id);
      return `<article class="mobile-row"><div class="mobile-row-top"><div class="person"><div class="avatar">${escapeHtml((user.nome||'U').slice(0,1).toUpperCase())}</div><div><strong>${escapeHtml(user.nome)}</strong><small>${escapeHtml(user.email)}</small></div></div><span class="badge ${roleLabel(user)==='Tesoureiro'?'paid':'closed'}">${roleLabel(user)}</span></div><div class="mobile-row-meta"><span>${debits.length} doações</span><button class="btn secondary" data-history="${user.id}">Histórico</button><button class="btn secondary" data-edit="${user.id}">Editar</button><button class="icon-btn" data-remove="${user.id}">×</button></div></article>`;
    };
    target.querySelector('#userRows').innerHTML=filtered.map(row).join('')||'<tr><td colspan="5"><div class="empty">Nenhum usuário encontrado.</div></td></tr>';
    target.querySelector('#userCards').innerHTML=filtered.map(card).join('')||'<div class="empty">Nenhum usuário encontrado.</div>';
    target.querySelectorAll('[data-edit]').forEach(button=>button.onclick=()=>window.amaActions.openUser(state.users.find(user=>user.id===Number(button.dataset.edit))));
    target.querySelectorAll('[data-history]').forEach(button=>button.onclick=()=>showHistory(state.users.find(user=>user.id===Number(button.dataset.history)),state));
    target.querySelectorAll('[data-remove]').forEach(button=>button.onclick=async()=>{const user=state.users.find(item=>item.id===Number(button.dataset.remove));if(!user||!confirm(`Remover ${user.nome}? Cadastros com doações registradas são preservados.`))return;try{await api.removeUser(user.id);toast('Usuário removido.');await refresh()}catch(error){toast(error.message)}});
  };
  target.querySelector('#userSearch').oninput=event=>{search=event.target.value;paint()};
  target.querySelector('#roleFilter').onchange=event=>{role=event.target.value;paint()};
  paint();
}

function showHistory(user,state){
  if(!user)return;
  const debits=state.debits.filter(debit=>debit.usuarioId===user.id).sort((a,b)=>String(b.mesReferencia||b.dataCriacao).localeCompare(String(a.mesReferencia||a.dataCriacao)));
  const content=debits.map(debit=>`<article class="history-row"><div><strong>${escapeHtml(debit.nomeTipoDoacao)}</strong><small>${monthLabel(debit.mesReferencia||debit.dataCriacao)}</small></div><div>${money(debit.valor)}<small>${badge(debit.status)}</small></div>${debit.urlComprovante?`<a href="${escapeHtml(debit.urlComprovante)}" target="_blank" rel="noopener">Comprovante ↗</a>`:'<span class="muted">Sem comprovante</span>'}</article>`).join('')||'<div class="empty">Este usuário ainda não possui doações.</div>';
  const dialog=modal(`Histórico · ${user.nome}`,`${roleLabel(user)} · ${user.email}`,`<div class="history-list">${content}</div>`,'<button class="btn ghost" data-close>Fechar</button>',true);
  dialog.querySelector('[data-close]').onclick=()=>dialog.remove();
}

const isPaid=debit=>String(debit.status)==='2'||String(debit.status).toLowerCase()==='pago';
const isRejected=debit=>String(debit.status)==='3'||String(debit.status).toLowerCase()==='rejeitado';

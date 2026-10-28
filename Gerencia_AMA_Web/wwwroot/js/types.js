import {api} from './api.js';
import {escapeHtml,monthLabel,dateKey,modal,toast,money} from './ui.js';

export function renderTypes(target,state,refresh){
  const legacyTypes=state.types.filter(type=>Number(type.valorPadrao)<=0);
  const typeRows=state.types.map(type=>{
    const scope=type.todosUsuarios?'Todos os usuários':`${(type.usuariosIds||[]).length} usuários`;
    return `<tr><td><strong>${escapeHtml(type.nome)}</strong><small class="type-description">${escapeHtml(type.descricao||'Sem descrição')}</small></td><td>${money(type.valorPadrao||0)}</td><td>${monthLabel(type.mesAbertura||type.dataCriacao)} – ${monthLabel(type.dataExpiracao)}</td><td>${scope}</td><td><div class="actions"><button class="btn secondary" data-edit="${type.id}">Editar</button><button class="icon-btn" data-delete="${type.id}" aria-label="Excluir tipo">×</button></div></td></tr>`;
  }).join('');
  const cards=state.types.map(type=>`<article class="mobile-row"><div class="mobile-row-top"><div><strong>${escapeHtml(type.nome)}</strong><small>${escapeHtml(type.descricao||'Sem descrição')}</small></div><strong>${money(type.valorPadrao||0)}</strong></div><div class="mobile-row-meta"><span>${monthLabel(type.mesAbertura||type.dataCriacao)} – ${monthLabel(type.dataExpiracao)} · ${type.todosUsuarios?'Todos os usuários':`${(type.usuariosIds||[]).length} selecionados`}</span><button class="btn secondary" data-edit="${type.id}">Editar</button><button class="icon-btn" data-delete="${type.id}">×</button></div></article>`).join('');
  target.innerHTML=`<section class="panel"><div class="panel-head"><div><h2 class="panel-title">Campanhas de doação</h2><div class="panel-note">Defina o valor mensal, o período e os membros que receberão cada débito.</div></div><span class="legend">${state.types.length} campanhas</span></div>${legacyTypes.length?`<div class="inline-warning">${legacyTypes.length} campanha(s) antiga(s) ainda precisam de valor mensal. Edite-as para habilitar a geração automática dos débitos.</div>`:''}<div class="table-wrap"><table><thead><tr><th>Campanha</th><th>Valor mensal</th><th>Período</th><th>Destinatários</th><th></th></tr></thead><tbody>${typeRows||'<tr><td colspan="5"><div class="empty">Ainda não há campanhas cadastradas.</div></td></tr>'}</tbody></table></div><div class="mobile-cards">${cards||'<div class="empty">Ainda não há campanhas cadastradas.</div>'}</div></section>`;
  target.querySelectorAll('[data-edit]').forEach(button=>button.onclick=()=>editType(state.types.find(t=>t.id===Number(button.dataset.edit)),state,refresh));
  target.querySelectorAll('[data-delete]').forEach(button=>button.onclick=async()=>{const type=state.types.find(t=>t.id===Number(button.dataset.delete));if(!type||!confirm(`Excluir “${type.nome}”?`))return;try{await api.deleteType(type.id);toast('Campanha excluída.');await refresh()}catch(error){toast(error.message)}});
}

export function editType(type,state,refresh){
  const members=state.users;
  const currentMonth=dateKey(new Date());
  const opening=type?.mesAbertura?dateKey(type.mesAbertura):currentMonth;
  const expiration=type?.dataExpiracao?dateKey(type.dataExpiracao):currentMonth;
  const selected=new Set(type?.usuariosIds||[]);
  const restricted=type?!type.todosUsuarios:false;
  const body=`<form id="typeForm"><div class="form-grid">
    <div class="form-field full"><label>Nome da campanha</label><input name="name" maxlength="100" required value="${escapeHtml(type?.nome||'')}"></div>
    <div class="form-field full"><label>Descrição</label><input name="description" maxlength="255" value="${escapeHtml(type?.descricao||'')}" placeholder="Objetivo desta contribuição"></div>
    <div class="form-field"><label>Valor mensal por membro (R$)</label><input name="amount" type="number" min="0.01" max="999999.99" step="0.01" required value="${type?.valorPadrao||''}" placeholder="0,00"><small>Usado para gerar os débitos automaticamente.</small></div>
    <div class="form-field"><label>Primeiro mês</label><input name="opens" type="month" value="${opening}" required></div>
    <div class="form-field"><label>Último mês</label><input name="expires" type="month" value="${expiration}" required></div>
    <div class="form-field full"><label>Quem recebe esta campanha?</label><select name="scope"><option value="all" ${!restricted?'selected':''}>Todos os usuários (inclui tesoureiros)</option><option value="selected" ${restricted?'selected':''}>Somente usuários selecionados</option></select></div>
    <div class="form-field full" id="memberPicker" ${restricted?'':'hidden'}><div class="member-picker-head"><strong>Selecione os usuários</strong><button type="button" class="btn ghost" id="selectAllMembers">Selecionar todos</button></div><input id="memberSearch" class="field" placeholder="Buscar por nome ou e-mail…"><div class="member-check-list">${members.map(user=>`<label class="member-check" data-search="${escapeHtml(`${user.nome} ${user.email}`.toLocaleLowerCase('pt-BR'))}"><input type="checkbox" name="members" value="${user.id}" ${selected.has(user.id)?'checked':''}><span><strong>${escapeHtml(user.nome)}${String(user.perfil)==='2'?' · Tesoureiro':''}</strong><small>${escapeHtml(user.email)}</small></span></label>`).join('')||'<div class="empty">Nenhum usuário cadastrado.</div>'}</div><small>Alterar a lista afeta somente os débitos futuros. O histórico existente é preservado.</small></div>
  </div></form>`;
  const dialog=modal(type?'Editar campanha':'Nova campanha','Os débitos são criados mensalmente e nunca duplicados para o mesmo membro e tipo.',body,'<button class="btn ghost" data-cancel>Cancelar</button><button class="btn" form="typeForm">Salvar campanha</button>',true);
  dialog.querySelector('[data-cancel]').onclick=()=>dialog.remove();
  const picker=dialog.querySelector('#memberPicker');
  dialog.querySelector('[name="scope"]').onchange=event=>picker.hidden=event.target.value!=='selected';
  dialog.querySelector('#selectAllMembers')?.addEventListener('click',()=>dialog.querySelectorAll('[name="members"]').forEach(input=>{if(input.closest('.member-check').hidden===false)input.checked=true}));
  dialog.querySelector('#memberSearch')?.addEventListener('input',event=>{const query=event.target.value.toLocaleLowerCase('pt-BR');dialog.querySelectorAll('.member-check').forEach(row=>row.hidden=!row.dataset.search.includes(query))});
  dialog.querySelector('#typeForm').onsubmit=async event=>{
    event.preventDefault();const data=new FormData(event.currentTarget),opens=data.get('opens'),ends=data.get('expires');
    if(opens>ends){toast('O primeiro mês precisa ser anterior ou igual ao último.');return}
    const todosUsuarios=data.get('scope')==='all';
    const usuariosIds=todosUsuarios?[]:[...dialog.querySelectorAll('[name="members"]:checked')].map(input=>Number(input.value));
    if(!todosUsuarios&&!usuariosIds.length){toast('Selecione pelo menos um usuário para esta campanha.');return}
    const payload={nome:data.get('name'),descricao:data.get('description')||null,valorPadrao:Number(data.get('amount')),todosUsuarios,usuariosIds,mesAbertura:`${opens}-01T00:00:00`,dataExpiracao:`${ends}-01T00:00:00`};
    try{if(type)await api.updateType(type.id,payload);else await api.createType(payload);dialog.remove();toast(type?'Campanha atualizada.':'Campanha criada.');await refresh()}catch(error){toast(error.message)}
  };
}

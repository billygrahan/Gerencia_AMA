import {activeInMonth,dateKey,monthLabel,money,escapeHtml} from './ui.js';

export async function renderCalendar(target,state){
  const year=Number(state.month.slice(0,4)),admin=state.isTreasurer;
  const yearDebits=state.debits.filter(d=>String(d.mesReferencia||d.dataCriacao).slice(0,4)===String(year));
  const paid=yearDebits.filter(isPaid),pending=yearDebits.filter(d=>!isPaid(d)&&!isRejected(d));
  const confirmed=paid.reduce((sum,d)=>sum+Number(d.valor),0);
  const uniqueMembers=new Set(yearDebits.map(d=>d.usuarioId)).size;
  const stats=admin?[
    ['Tipos cadastrados',state.types.length,'Campanhas no sistema','▦'],
    [`Pagas em ${year}`,paid.length,`${money(confirmed)} confirmados`,'✓'],
    ['Pendentes',pending.length,'Aguardando comprovante ou conferência','◷'],
    ['Membros com lançamentos',uniqueMembers,`De ${state.users.length} cadastrados`,'♙']
  ]:[
    ['Tipos cadastrados',state.types.length,'Campanhas da igreja','▦'],
    [`Minhas doações`,yearDebits.length,`No ano ${year}`,'♧'],
    ['Em aberto',pending.length,'Aguardando análise','◷'],
    ['Valor confirmado',money(confirmed),'Doações aprovadas','✓']
  ];
  target.innerHTML=`<div class="grid stats">${stats.map(([title,value,hint,icon])=>`<article class="stat"><span class="stat-icon">${icon}</span><div class="stat-label">${title}</div><div class="stat-value">${value}</div><div class="stat-hint">${hint}</div></article>`).join('')}</div>${admin?`<section class="admin-shortcuts"><button data-go="donations"><span>♧</span><strong>Revisar doações</strong><small>Comprovantes e pendências</small><b>›</b></button><button data-go="types"><span>✳</span><strong>Campanhas</strong><small>Valores, período e membros</small><b>›</b></button><button data-go="users"><span>♙</span><strong>Membros</strong><small>Cadastros e perfis de acesso</small><b>›</b></button></section>`:''}<section class="panel"><div class="panel-head"><div><h2 class="panel-title">Campanhas por mês</h2><div class="panel-note">Selecione um mês para conferir o que está aberto, pendente ou encerrado.</div></div><div class="legend"><span><i class="dot open"></i>Aberta, sem lançamento</span><span><i class="dot pending"></i>Com lançamento</span><span><i class="dot closed"></i>Não abriu / encerrada</span></div></div><div class="calendar" id="calendarGrid"></div></section><section class="panel"><div class="panel-head"><div><h2 class="panel-title">Resumo de ${year}</h2><div class="panel-note">Toque em um mês para abrir suas doações e comprovantes.</div></div><button class="btn secondary" id="seeAll">${admin?'Ver doações':'Ver minhas doações'}</button></div><div class="table-wrap"><table><thead><tr><th>Mês</th><th>Registros</th><th>Pagas</th><th>Pendentes</th><th>Valor confirmado</th></tr></thead><tbody>${Array.from({length:12},(_,i)=>{const key=`${year}-${String(i+1).padStart(2,'0')}`,records=yearDebits.filter(d=>monthKey(d)===key),done=records.filter(isPaid),wait=records.filter(d=>!isPaid(d)&&!isRejected(d));return `<tr data-month="${key}"><td><strong>${monthLabel(key)}</strong></td><td>${records.length}</td><td>${done.length}</td><td>${wait.length}</td><td>${money(done.reduce((sum,d)=>sum+Number(d.valor),0))}</td></tr>`}).join('')}</tbody></table></div></section>`;
  target.querySelectorAll('[data-go]').forEach(button=>button.onclick=()=>window.amaGo(button.dataset.go));
  const grid=target.querySelector('#calendarGrid');
  grid.innerHTML=Array.from({length:12},(_,i)=>{
    const month=`${year}-${String(i+1).padStart(2,'0')}`,monthDate=new Date(`${month}-01T12:00:00`),records=yearDebits.filter(d=>monthKey(d)===month);
    const items=state.types.map(type=>{
      const donations=records.filter(d=>d.tipoDoacaoId===type.id),done=donations.filter(isPaid).length,wait=donations.filter(d=>!isPaid(d)&&!isRejected(d)).length;
      let label,cls,description;
      if(activeInMonth(type,month)){
        cls=donations.length?'pending':'open';
        if(admin){const recipients=type.todosUsuarios?state.users.length:(type.usuariosIds||[]).length;label=donations.length?`${done} pago · ${wait} pend.`:`${recipients} usuário(s)`;description=`Disponível para ${recipients} usuário(s); ${done} pago(s) e ${wait} pendente(s)`;}
        else{label=donations.length?`${done} pago · ${wait} pend.`:'aberta';description=donations.length?`${done} pagamento(s) aprovado(s), ${wait} pendente(s)`:'Aberta, ainda sem lançamento';}
      }
      else{cls='closed';label=month<dateKey(type.mesAbertura||type.dataCriacao)?'não abriu':'encerrada';description=label==='não abriu'?'Ainda não está aberta neste mês':'Campanha encerrada';}
      return `<div class="month-item" title="${escapeHtml(type.nome)}: ${description}"><i class="dot ${cls}"></i><span class="month-type-name">${escapeHtml(type.nome)}</span><span class="month-state ${cls}">${label}</span></div>`;
    });
    return `<button class="month-card ${month===state.month?'current':''}" data-select-month="${month}"><div class="month-top"><span class="month-name">${monthLabel(month)}</span><span class="month-count">${records.length} lanç.</span></div><div class="month-items">${items.join('')||'<div class="month-empty">Nenhum tipo cadastrado</div>'}</div></button>`;
  }).join('');
  const selectMonth=month=>{state.month=month;document.querySelector('#monthPicker').value=month;window.amaGo('donations')};
  grid.querySelectorAll('[data-select-month]').forEach(card=>card.onclick=()=>selectMonth(card.dataset.selectMonth));
  target.querySelector('#seeAll').onclick=()=>window.amaGo('donations');
  target.querySelectorAll('tbody tr[data-month]').forEach(row=>row.onclick=()=>selectMonth(row.dataset.month));
}

const isPaid=d=>String(d.status)==='2'||String(d.status).toLowerCase()==='pago';
const isRejected=d=>String(d.status)==='3'||String(d.status).toLowerCase()==='rejeitado';
const monthKey=d=>String(d.mesReferencia||d.dataCriacao).slice(0,7);

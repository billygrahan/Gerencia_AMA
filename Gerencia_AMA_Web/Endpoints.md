# 📖 Documentação da API - Gerência AMA Web

Esta documentação detalha todos os endpoints HTTP disponíveis no sistema de gerência de tesouraria, com suas respectivas regras de acesso, parâmetros e respostas.

---

## 🔐 1. Módulo de Autenticação e Conta (/api/Auth)

### POST /api/Auth/register
- Descrição: Permite o autocadastro público de novos usuários na plataforma. Por padrão, todo novo registro é associado ao perfil de Membro.
- Acesso: Público (Não exige Token JWT).
- Corpo da Requisição (CriarUsuarioDto):
  {
    "nome": "Nome do Usuário",
    "email": "usuario@email.com",
    "senha": "SuaSenhaSegura123"
  }
- Retorno (201 Created): Objeto com as informações do usuário criado.

---

### POST /api/Auth/login
- Descrição: Autentica o usuário no sistema validando as credenciais de e-mail e senha enviadas.
- Acesso: Público (Não exige Token JWT).
- Corpo da Requisição (LoginDto):
  {
    "email": "usuario@email.com",
    "senha": "SuaSenhaSegura123"
  }
- Retorno (200 OK): Retorna o accessToken (validade de 15 min), refreshToken (validade de 7 dias), expiração e dados básicos do usuário.

---

### POST /api/Auth/refresh-token
- Descrição: Renova o accessToken expirado sem solicitar as credenciais do usuário novamente, utilizando o refreshToken salvo no dispositivo.
- Acesso: Público (Não exige Token JWT).
- Corpo da Requisição (RefreshTokenDto):
  {
    "accessToken": "token_expirado_aqui",
    "refreshToken": "refresh_token_valido_aqui"
  }
- Retorno (200 OK): Novo par de accessToken e refreshToken atualizados.

---

### GET /api/Auth/me
- Descrição: Obtém as informações do perfil do próprio usuário que está conectado, extraindo o ID diretamente do Token JWT enviado no cabeçalho Authorization.
- Acesso: Autenticado (Requer Token JWT de qualquer perfil).
- Retorno (200 OK): Dados do perfil do usuário logado (id, nome, email, perfil).

---

### PUT /api/Auth/me
- Descrição: Permite que o usuário conectado altere seus próprios dados cadastrais (Nome, E-mail ou Senha). O usuário é impedido de alterar seu próprio perfil/role (Membro/Tesoureiro).
- Acesso: Autenticado (Requer Token JWT do próprio usuário).
- Corpo da Requisição (AtualizarMeuPerfilDto):
  {
    "nome": "Nome Atualizado",
    "email": "novoemail@email.com",
    "novaSenha": "SenhaOpcional123"
  }
- Retorno (204 No Content / 200 OK): Confirmação de alteração realizada com sucesso.

---

## 💳 2. Módulo de Débitos dos Membros (/api/DebitosMembros)

### GET /api/DebitosMembros
- Descrição: Lista os débitos cadastrados no sistema.
- Acesso: Restrito ao Tesoureiro. Apesar de o método conter uma filtragem para membros, a autorização aplicada ao endpoint impede que membros o acessem; para consultar os próprios débitos, use `GET /api/DebitosMembros/me`.
- Retorno (200 OK): Lista de objetos de débitos contendo valor, status (EmAberto, Pago, Rejeitado), datas, usuário e comprovante vinculado.

---

### POST /api/DebitosMembros
- Descrição: Cria um novo débito pendente para um usuário específico. Exclusivo para gestão administrativa.
- Acesso: Restrito ao Tesoureiro.
- Corpo da Requisição (CriarDebitoMembroDto):
  {
    "usuarioId": 1,
    "tipoDoacaoId": 2,
    "valor": 150.00
  }
- Retorno (201 Created): Objeto do débito criado com status inicial EmAberto.

---

### GET /api/DebitosMembros/{id}
- Descrição: Detalha as informações de um débito específico buscando por seu identificador (Id).
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Retorno (200 OK): Objeto do débito correspondente ou 404 Not Found.

---

### GET /api/DebitosMembros/me
- Descrição: Retorna a lista de débitos pertencentes exclusivamente ao membro autenticado na requisição.
- Acesso: Autenticado (Qualquer perfil).
- Retorno (200 OK): Lista de débitos do usuário logado.

---

### POST /api/DebitosMembros/meus-debitos
- Descrição: Permite que o próprio membro crie uma intenção de débito/doação vinculada à sua conta sem precisar selecionar o UsuarioId.
- Acesso: Autenticado (Qualquer perfil, inclusive Tesoureiro).
- Corpo da Requisição (CriarMeuDebitoDto):
  {
    "tipoDoacaoId": 2,
    "valor": 50.00
  }
- Retorno (201 Created): Objeto do débito criado associado ao ID do token.

---

### POST /api/DebitosMembros/{id}/anexar-comprovante
- Descrição: Permite ao Tesoureiro enviar ou substituir a imagem (JPG, PNG) ou documento (PDF) de pagamento de qualquer débito. Pode ser executado se o débito estiver EmAberto ou Rejeitado e altera automaticamente o status para Pago.
- Acesso: Restrito ao Tesoureiro.
- Formato: multipart/form-data
- Parâmetros: id na rota e campo Arquivo contendo o documento/imagem.
- Retorno (200 OK): Link público retornado pelo Cloudinary e o novo status atualizado.

---

### POST /api/DebitosMembros/meus-debitos/{id}/anexar-comprovante
- Descrição: Permite ao usuário enviar ou substituir o comprovante do próprio débito. Pode ser executado se o débito estiver EmAberto ou Rejeitado e altera automaticamente o status para Pago.
- Acesso: Autenticado. O débito deve pertencer ao usuário identificado pelo token JWT; caso contrário, a API retorna `403 Forbidden`.
- Formato: multipart/form-data
- Parâmetros: id na rota e campo Arquivo contendo o documento/imagem.
- Retorno (200 OK): Link público retornado pelo Cloudinary e o novo status atualizado.

---

### PATCH /api/DebitosMembros/{id}/rejeitar
- Descrição: Permite que o Tesoureiro reprove um comprovante incorreto ou ilegível, marcando o status do débito como Rejeitado para que o membro possa enviar um novo arquivo.
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Retorno (200 OK): Mensagem de confirmação de alteração para Rejeitado.

---

## 👤 3. Módulo de Membros (/api/Membros)

### GET /api/Membros/me
- Descrição: Retorna os dados básicos da conta do usuário logado (id, nome, e-mail e perfil). Não há uma ficha de membro separada implementada neste endpoint.
- Acesso: Autenticado.
- Retorno (200 OK): Dados básicos da conta (id, nome, e-mail e perfil).

---

### PUT /api/Membros/me
- Descrição: Atualiza os dados básicos da conta do usuário atual (nome, e-mail e, opcionalmente, senha). O perfil/role não pode ser alterado por este endpoint.
- Acesso: Autenticado.
- Retorno (204 No Content): Confirmação de atualização.

---

## 🏷️ 4. Módulo de Tipos de Doação (/api/TiposDoacao)

### GET /api/TiposDoacao
- Descrição: Lista todos os tipos e campanhas de doação cadastradas (ex: Dízimo, Oferta de Missões, Construção, etc.).
- Acesso: Autenticado (Disponível para Membros e Tesoureiros).
- Retorno (200 OK): Lista contendo id, nome, dataCriacao, dataExpiracao e descricao.

---

### POST /api/TiposDoacao
- Descrição: Cadastra uma nova categoria ou campanha de doação no sistema.
- Acesso: Restrito ao Tesoureiro.
- Corpo da Requisição (CriarTipoDoacaoDto):
  {
    "nome": "Oferta de Missões",
    "descricao": "Fundo destinado ao suporte missionário",
    "dataExpiracao": "2026-12-31T23:59:59Z"
  }
- Retorno (201 Created): Objeto cadastrado.

---

### GET /api/TiposDoacao/{id}
- Descrição: Obtém as informações detalhadas de um tipo de doação específico por seu ID.
- Acesso: Autenticado.
- Parâmetros de Rota: id (int).
- Retorno (200 OK): Objeto com as propriedades da categoria de doação.

---

### PUT /api/TiposDoacao/{id}
- Descrição: Atualiza o nome, descrição ou data de expiração de um tipo de doação existente.
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Corpo da Requisição (AtualizarTipoDoacaoDto):
  {
    "nome": "Oferta de Missões 2026",
    "descricao": "Fundo atualizado",
    "dataExpiracao": "2026-12-31T23:59:59Z"
  }
- Retorno (204 No Content): Sucesso sem corpo de retorno.

---

### DELETE /api/TiposDoacao/{id}
- Descrição: Remove um tipo de doação do banco de dados.
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Retorno (204 No Content): Confirmação de exclusão.

---

## ⚙️ 5. Módulo Administrativo de Usuários (/api/Usuarios)

### GET /api/Usuarios
- Descrição: Lista todos os usuários (membros e tesoureiros) cadastrados na plataforma.
- Acesso: Restrito ao Tesoureiro.
- Retorno (200 OK): Array com a lista de usuários e seus respectivos papéis no sistema (Membro / Tesoureiro).

---

### POST /api/Usuarios
- Descrição: Permite que o Tesoureiro cadastre diretamente um usuário via painel administrativo.
- Acesso: Restrito ao Tesoureiro.
- Corpo da Requisição (CriarUsuarioDto). O perfil é opcional e assume Membro por padrão:
  {
    "nome": "Novo Membro",
    "email": "membro@email.com",
    "senha": "SenhaIniciais123",
    "perfil": 1
  }
- Retorno (201 Created): Usuário criado.

---

### GET /api/Usuarios/{id}
- Descrição: Consulta o cadastro completo de qualquer usuário especificando o ID.
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Retorno (200 OK): Dados do usuário consultado.

---

### PUT /api/Usuarios/{id}
- Descrição: Permite ao Tesoureiro alterar dados de outros usuários, incluindo a alteração de perfil/role (promover Membro a Tesoureiro ou vice-versa).
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Corpo da Requisição (AtualizarUsuarioDto):
  {
    "nome": "Nome Corrigido",
    "email": "email@email.com",
    "perfil": 2
  }
- Retorno (204 No Content): Sucesso na alteração.

---

## Atualizações de doações por mês

Doações agora possuem `mesReferencia` (primeiro dia do mês no formato ISO). O servidor usa o mês atual quando o campo não for enviado. A combinação membro + tipo de doação + mês é única; uma segunda tentativa retorna `409 Conflict`. Tipos só podem receber lançamentos entre o mês de criação e o mês de expiração, inclusive.

### POST /api/DebitosMembros
- Inclua `mesReferencia` junto a `usuarioId`, `tipoDoacaoId` e `valor`.

### POST /api/DebitosMembros/meus-debitos
- Inclua `mesReferencia` junto a `tipoDoacaoId` e `valor`.

Os retornos de doação incluem `mesReferencia`. Em registros antigos, esse campo permanece nulo; o cliente deve apresentar o mês de `dataCriacao` como referência histórica.

### PATCH /api/DebitosMembros/{id}/aprovar
- Acesso: Restrito ao Tesoureiro.
- Aprova um pagamento que possui comprovante anexado e altera seu status para `Pago`.

O envio de comprovante mantém o status `EmAberto` até revisão da tesouraria. A rota existente `PATCH /api/DebitosMembros/{id}/rejeitar` continua disponível.

---

### DELETE /api/Usuarios/{id}
- Descrição: Exclui permanentemente uma conta de usuário do sistema.
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: id (int).
- Retorno (204 No Content): Sucesso na exclusão.

---

### GET /api/Usuarios/TipoDoacoes/{UsuarioId}
- Descrição: Lista os tipos de doações que possuem histórico ou débitos associados a um determinado usuário (UsuarioId).
- Acesso: Restrito ao Tesoureiro.
- Parâmetros de Rota: UsuarioId (int).
- Retorno (200 OK): Lista dos tipos de doação relacionados ao membro selecionado.

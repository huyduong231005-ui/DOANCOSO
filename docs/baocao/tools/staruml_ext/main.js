/* global app */
// Dựng sơ đồ UML / ERD trong StarUML từ một file mô tả JSON, bằng chính factory của StarUML.
// Gọi qua lệnh baocao:build (staruml_cli.py chạy lệnh này bằng `StarUML.exe ejs ...`).
// spec = { project, output, diagrams: [{ kind, model, name, nodes: [...], edges: [...] }] }
// kind: usecase | activity | state | erd | class | sequence | deployment

const fs = require('fs')
const { Point } = app.type

const FONT = 'Times New Roman'

// Trắng đen, Times New Roman cho cả view và mọi nhãn con (nhãn «include», tên luồng, bản số...).
function style (v, size) {
  if (v.font) {
    v.font.face = FONT
    v.font.size = size || 13
  }
  v.showShadow = false
  v.fillColor = '#ffffff'
  v.lineColor = '#000000'
  v.fontColor = '#000000'
  for (const sv of v.subViews || []) style(sv, size)
}

const NODE_ID = {
  actor: 'UMLActor', usecase: 'UMLUseCase', subject: 'UMLUseCaseSubject',
  partition: 'UMLActivityPartition', action: 'UMLAction', initial: 'UMLInitialNode',
  final: 'UMLActivityFinalNode', decision: 'UMLDecisionNode', merge: 'UMLMergeNode',
  fork: 'UMLForkNode', join: 'UMLJoinNode',
  state: 'UMLState', pseudo: 'UMLPseudostate', finalstate: 'UMLFinalState',
  entity: 'ERDEntity', note: 'Note',
  class: 'UMLClass', interface: 'UMLInterface', enum: 'UMLEnumeration',
  lifeline: 'UMLLifeline', fragment: 'UMLCombinedFragment',
  node: 'UMLNode', component: 'UMLComponent', artifact: 'UMLArtifact',
  nary: 'UMLNaryAssociationNode', text: 'Text', fcdecision: 'FCDecision', fcprocess: 'FCProcess'
}
const EDGE_ID = {
  assoc: 'UMLAssociation', include: 'UMLInclude', extend: 'UMLExtend', generalization: 'UMLGeneralization',
  flow: 'UMLControlFlow', transition: 'UMLTransition', relation: 'ERDRelationship',
  dependency: 'UMLDependency', realization: 'UMLInterfaceRealization', commpath: 'UMLCommunicationPath',
  message: 'UMLMessage', classlink: 'UMLAssociationClassLink', fcflow: 'FCFlow', notelink: 'NoteLink'
}
const DIAGRAM_ID = {
  usecase: 'UMLUseCaseDiagram', activity: 'UMLActivityDiagram', state: 'UMLStatechartDiagram',
  class: 'UMLClassDiagram', sequence: 'UMLSequenceDiagram', deployment: 'UMLDeploymentDiagram',
  flowchart: 'FCFlowchartDiagram'
}

function makeDiagram (project, d) {
  if (d.kind === 'erd') {
    const dm = app.factory.createModel({ id: 'ERDDataModel', parent: project, modelInitializer: m => { m.name = d.model } })
    return app.factory.createDiagram({
      id: 'ERDDiagram', parent: dm, diagramInitializer: g => { g.name = d.title || d.name; g.documentation = d.name }
    })
  }
  const model = app.factory.createModel({ id: 'UMLModel', parent: project, modelInitializer: m => { m.name = d.model } })
  // Tên hiển thị (khung sd, Model Explorer) là d.title; d.name (không dấu) dùng làm tên file ảnh xuất ra.
  const diagram = app.factory.createDiagram({
    id: DIAGRAM_ID[d.kind], parent: model, diagramInitializer: g => { g.name = d.title || d.name; g.documentation = d.name }
  })
  // Activity / StateMachine / Interaction do StarUML tự tạo: đặt tên cho dễ tìm trong Model Explorer.
  if (d.kind === 'activity' || d.kind === 'state') app.engine.setProperty(diagram._parent, 'name', d.owner || d.name)
  if (d.kind === 'sequence') {
    app.engine.setProperty(diagram._parent, 'name', d.owner || d.name)
    app.engine.setProperty(diagram._parent._parent, 'name', d.owner || d.name)
  }
  return diagram
}

function addColumns (entity, cols) {
  for (const c of cols) {
    app.factory.createModel({
      id: 'ERDColumn',
      parent: entity,
      field: 'columns',
      modelInitializer: m => {
        m.name = c.name
        m.type = c.type || ''
        m.primaryKey = !!c.pk
        m.foreignKey = !!c.fk
        m.nullable = !!c.nullable
        m.unique = !!c.unique
      }
    })
  }
}

// Thuộc tính, thao tác (kèm tham số, kiểu trả về) và giá trị liệt kê của lớp.
function addFeatures (cls, n) {
  for (const a of n.attrs || []) {
    app.factory.createModel({
      id: 'UMLAttribute',
      parent: cls,
      field: 'attributes',
      modelInitializer: m => {
        m.name = a.name
        m.type = a.type || ''
        m.visibility = a.vis || 'public'
        m.isStatic = !!a.static
        if (a.mult) m.multiplicity = a.mult
      }
    })
  }
  for (const o of n.ops || []) {
    const op = app.factory.createModel({
      id: 'UMLOperation',
      parent: cls,
      field: 'operations',
      modelInitializer: m => {
        m.name = o.name
        m.visibility = o.vis || 'public'
        m.isStatic = !!o.static
      }
    })
    const params = (o.params || []).map(([name, type]) => ({ name, type, direction: 'in' }))
    if (o.ret) params.push({ name: '', type: o.ret, direction: 'return' })
    for (const p of params) {
      app.factory.createModel({
        id: 'UMLParameter',
        parent: op,
        field: 'parameters',
        modelInitializer: m => { m.name = p.name; m.type = p.type; m.direction = p.direction }
      })
    }
  }
  for (const lit of n.literals || []) {
    app.factory.createModel({
      id: 'UMLEnumerationLiteral', parent: cls, field: 'literals', modelInitializer: m => { m.name = lit }
    })
  }
}

function center (v) {
  return v.getCenter ? v.getCenter() : { x: v.left, y: v.top }
}

function buildNode (diagram, owner, n, d) {
  const opts = {
    id: NODE_ID[n.kind],
    parent: owner,
    diagram,
    x1: n.x, y1: n.y, x2: n.x + (n.w || 0), y2: n.y + (n.h || 0),
    modelInitializer: m => {
      if (n.name !== undefined) m.name = n.name
      else if (n.kind === 'fragment') m.name = ''
      if (n.stereotype) m.stereotype = n.stereotype
      if (n.abstract) m.isAbstract = true
      if (n.kind === 'fragment') {
        m.interactionOperator = n.operator
        m.operands[0].guard = (n.guards || [''])[0]
      }
    },
    viewInitializer: v => { style(v, n.size || d.fontSize) }
  }
  if (n.kind === 'partition') opts['view-init'] = { isVertical: n.horizontal ? false : true }
  if (n.kind === 'pseudo') opts.pseudostateKind = 'initial'
  if (n.kind === 'note' || n.kind === 'text') {
    opts.modelInitializer = null
    opts.viewInitializer = v => { style(v, n.size || d.fontSize); v.text = n.name }
  }
  const v = app.factory.createModelAndView(opts)
  if (n.kind === 'entity') addColumns(v.model, n.columns || [])
  if (n.kind === 'class' || n.kind === 'interface' || n.kind === 'enum') addFeatures(v.model, n)
  if (n.kind === 'fragment') {
    for (const g of (n.guards || []).slice(1)) {
      app.factory.createModel({
        id: 'UMLInteractionOperand', parent: v.model, field: 'operands', modelInitializer: m => { m.guard = g }
      })
    }
    // StarUML cho mỗi nhánh cao 43px cố định; chia lại theo các mốc y tuyệt đối của đường phân cách.
    if (n.splits) {
      const comp = v.operandCompartment || v.subViews.find(s => s instanceof app.type.UMLInteractionOperandCompartmentView)
      const top = n.y + 22 // ngăn nhánh nằm dưới nhãn toán tử 22px
      const marks = [top, ...n.splits, n.y + n.h]
      comp.subViews.forEach((op, i) => app.engine.setProperty(op, 'height', marks[i + 1] - marks[i]))
    }
  }
  if (n.kind === 'class' || n.kind === 'interface' || n.kind === 'enum') {
    // Không hiện ngăn thao tác rỗng (lớp thực thể chỉ có thuộc tính).
    // Interface mặc định ẩn ngăn thao tác, nên luôn đặt tường minh theo spec.
    app.engine.setProperty(v, 'suppressOperations', !(n.ops || []).length)
    // Interface vẽ dạng hộp có «interface» (mặc định StarUML vẽ hình tròn), khung lớp tự co theo nội dung.
    if ('stereotypeDisplay' in v) app.engine.setProperty(v, 'stereotypeDisplay', 'label')
    app.engine.setProperty(v, 'autoResize', true)
    if (n.kind === 'enum' || !(n.attrs || []).length) app.engine.setProperty(v, 'suppressAttributes', !(n.attrs || []).length)
  }
  // Kích thước cố định theo spec; nếu bỏ trống, StarUML tự co theo nội dung.
  // autosize: w/h chỉ dùng để tính đường nối, khung lớp vẫn tự co theo nội dung.
  if (n.w && !n.autosize) {
    // Xuống dòng trong khung cố định thay vì kéo dài khung theo tên.
    const wrapKinds = ['action', 'usecase', 'state', 'component', 'node', 'artifact']
    if (wrapKinds.includes(n.kind) && 'wordWrap' in v) app.engine.setProperty(v, 'wordWrap', true)
    app.engine.setProperty(v, 'autoResize', false)
    app.engine.setProperty(v, 'width', n.w)
    app.engine.setProperty(v, 'height', n.h)
  }
  if (n.kind === 'class' && n.hideVisibility) app.engine.setProperty(v, 'showVisibility', false)
  // Hình thoi mối kết hợp (ERD): tên đặt giữa hình thoi thay vì nhãn trôi phía trên.
  if (n.kind === 'nary' && v.nameLabel) {
    app.engine.setProperty(v, 'showVisibility', false)
    v.nameLabel.font.size = n.size || d.fontSize || 13
    app.engine.setProperty(v.nameLabel, 'distance', 0)
  }
  return v
}

function buildEdge (diagram, owner, views, e, d) {
  const t = views[e.from]
  const h = views[e.to]
  if (!t || !h) throw new Error(`Thiếu đầu nối ${e.from} -> ${e.to}`)
  const tc = center(t)
  const hc = center(h)
  const opts = {
    id: EDGE_ID[e.kind],
    parent: owner,
    diagram,
    tailView: t, headView: h, tailModel: t.model, headModel: h.model,
    x1: tc.x, y1: e.y !== undefined ? e.y : tc.y, x2: hc.x, y2: e.y !== undefined ? e.y : hc.y,
    modelInitializer: m => {
      if (e.name) m.name = e.name
      if (e.guard) m.guard = e.guard
      if (e.stereotype) m.stereotype = e.stereotype
      if (e.kind === 'message') m.messageSort = e.sort || 'synchCall'
      if (e.kind === 'relation') {
        m.end1.cardinality = e.card1 || '1'
        m.end2.cardinality = e.card2 || '0..*'
        if (e.identifying) m.identifying = true
      }
      if (e.kind === 'assoc') {
        m.end1.navigable = e.nav1 || 'unspecified'
        m.end2.navigable = e.nav2 || 'unspecified'
        if (e.mult1) m.end1.multiplicity = e.mult1
        if (e.mult2) m.end2.multiplicity = e.mult2
        if (e.agg1) m.end1.aggregation = e.agg1
        if (e.role1) m.end1.name = e.role1
        if (e.role2) m.end2.name = e.role2
      }
      if (e.kind === 'commpath') {
        if (e.mult1) m.end1.multiplicity = e.mult1
        if (e.mult2) m.end2.multiplicity = e.mult2
      }
    },
    viewInitializer: v => {
      style(v, e.size || d.fontSize)
      if ('showVisibility' in v) v.showVisibility = false
      if (e.kind !== 'message') v.lineStyle = e.lineStyle === undefined ? 0 : e.lineStyle // 0 = vuông góc, 1 = xiên
      if (e.points) {
        v.points.clear()
        for (const [x, y] of e.points) v.points.add(new Point(x, y))
      }
    }
  }
  const relKinds = ['relation', 'assoc', 'include', 'extend', 'generalization', 'dependency', 'realization', 'commpath']
  if (relKinds.includes(e.kind)) opts.parent = t.model
  const ev = app.factory.createModelAndView(opts)
  // Nhãn của đường (tên chuyển trạng thái, điều kiện rẽ nhánh): đẩy ra xa đường kẻ để chữ không bị gạch ngang.
  if (ev && ev.nameLabel && e.labelDistance !== undefined) app.engine.setProperty(ev.nameLabel, 'distance', e.labelDistance)
  if (ev && ev.nameLabel && e.labelAlpha !== undefined) app.engine.setProperty(ev.nameLabel, 'alpha', e.labelAlpha)
  if (ev && ev.nameLabel && e.labelEdgePosition !== undefined) app.engine.setProperty(ev.nameLabel, 'edgePosition', e.labelEdgePosition)
  // Thanh kích hoạt kéo dài tới lúc trả về: mọi lời gọi tiếp theo của đối tượng đi ra từ thanh này.
  if (e.kind === 'message' && e.activationHeight && ev && ev.activation) {
    app.engine.setProperty(ev.activation, 'height', e.activationHeight)
  }
  return ev
}

function buildDiagram (project, d) {
  const diagram = makeDiagram(project, d)
  const owner = diagram._parent
  const views = {}
  for (const n of d.nodes) views[n.key] = buildNode(diagram, owner, n, d)
  for (const e of d.edges) buildEdge(diagram, owner, views, e, d)
  // Khung "sd" của sơ đồ tuần tự: StarUML tạo với kích thước mặc định, chỉnh cho vừa nội dung.
  if (d.frame) {
    const frame = diagram.ownedViews.find(v => v instanceof app.type.UMLFrameView)
    if (frame) {
      style(frame, d.fontSize)
      for (const [k, val] of Object.entries(d.frame)) app.engine.setProperty(frame, k, val)
    }
  }
}

// StarUML CLI không in được log khi thoát nhanh, nên ghi nhật ký ra file cạnh spec.
function logger (specPath) {
  const file = specPath + '.log'
  fs.writeFileSync(file, '')
  return msg => fs.appendFileSync(file, msg + '\n')
}

function build (specPath) {
  const log = logger(specPath)
  try {
    const spec = JSON.parse(fs.readFileSync(specPath, 'utf8'))
    const project = app.project.getProject()
    app.engine.setProperty(project, 'name', spec.project)
    for (const d of spec.diagrams) {
      try {
        buildDiagram(project, d)
        log(`ok ${d.name}`)
      } catch (err) {
        log(`LOI ${d.name}: ${err.stack}`)
      }
    }
    app.project.save(spec.output)
    log(`saved ${spec.output}`)
  } catch (err) {
    log(`LOI: ${err.stack}`)
  }
}

function init () {
  app.commands.register('baocao:build', build)
}

exports.init = init

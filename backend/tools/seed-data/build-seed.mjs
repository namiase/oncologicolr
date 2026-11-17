// Genera los JSON de datos semilla a partir de las fuentes oficiales descargadas.
// Uso: node build-seed.mjs <carpeta-descargas> <carpeta-salida>
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import path from 'node:path'

const [src, out] = process.argv.slice(2)
const read = (f) => JSON.parse(readFileSync(path.join(src, f), 'utf8'))
const collator = new Intl.Collator('es', { sensitivity: 'base' })
const byName = (a, b) => collator.compare(a.name, b.name)
mkdirSync(out, { recursive: true })
const write = (f, data) => writeFileSync(path.join(out, f), JSON.stringify(data, null, 1) + '\n')
const assert = (cond, msg) => { if (!cond) throw new Error('Validación fallida: ' + msg) }

// ---------- Países (ISO 3166-1 + nombres en español de CLDR) ----------
const dn = new Intl.DisplayNames(['es'], { type: 'region', fallback: 'none' })
const countries = read('iso/iso3166.json').map((c) => ({ code: c['alpha-2'], name: dn.of(c['alpha-2']) })).sort(byName)
assert(countries.length === 249 && countries.every((c) => c.name), 'países')
write('countries.json', countries)

// ---------- Venezuela ----------
const veIso = {
  'Amazonas': 'VE-Z', 'Anzoátegui': 'VE-B', 'Apure': 'VE-C', 'Aragua': 'VE-D', 'Barinas': 'VE-E', 'Bolívar': 'VE-F',
  'Carabobo': 'VE-G', 'Cojedes': 'VE-H', 'Delta Amacuro': 'VE-Y', 'Dependencias Federales': 'VE-W', 'Distrito Capital': 'VE-A',
  'Falcón': 'VE-I', 'Guárico': 'VE-J', 'La Guaira': 'VE-X', 'Lara': 'VE-K', 'Mérida': 'VE-L', 'Miranda': 'VE-M',
  'Monagas': 'VE-N', 'Nueva Esparta': 'VE-O', 'Portuguesa': 'VE-P', 'Sucre': 'VE-R', 'Táchira': 'VE-S', 'Trujillo': 'VE-T',
  'Yaracuy': 'VE-U', 'Zulia': 'VE-V',
}
// Correcciones de erratas del dataset de origen.
const veFixes = { 'Píar': 'Piar' }
const veSource = new Map(read('ve/venezuela.json').map((e) => [e.estado === 'Vargas' ? 'La Guaira' : e.estado, e]))
const veStates = Object.entries(veIso).map(([name, code]) => ({
  code,
  name,
  municipalities: (veSource.get(name)?.municipios ?? []).map((m) => ({ code: null, name: (veFixes[m.municipio.trim()] ?? m.municipio.trim()) })).sort(byName),
}))
assert([...veSource.keys()].every((k) => k in veIso), 'estados VE sin código: ' + [...veSource.keys()].filter((k) => !(k in veIso)))
assert(veStates.reduce((n, s) => n + s.municipalities.length, 0) === 335, 'municipios VE = 335')
write('geo-ve.json', { countryCode: 'VE', states: veStates })

// ---------- Brasil (IBGE) ----------
const brStates = read('br/estados.json').map((e) => ({ code: e.sigla, name: e.nome, municipalities: [] }))
const brBySigla = new Map(brStates.map((s) => [s.code, s]))
for (const m of read('br/municipios.json')) {
  const uf = m.microrregiao?.mesorregiao?.UF?.sigla ?? m['regiao-imediata']?.['regiao-intermediaria']?.UF?.sigla
  assert(brBySigla.has(uf), 'UF del municipio ' + m.nome)
  brBySigla.get(uf).municipalities.push({ code: String(m.id), name: m.nome.trim() })
}
brStates.forEach((s) => s.municipalities.sort(byName))
brStates.sort(byName)
assert(brStates.length === 27, 'estados BR = 27')
write('geo-br.json', { countryCode: 'BR', states: brStates })

// ---------- Colombia (DANE / DIVIPOLA) — nombres en MAYÚSCULAS → formato título en español ----------
const particles = new Set(['de', 'del', 'la', 'las', 'los', 'y', 'e', 'el', 'en', 'al', 'a'])
export const titleCase = (value) => {
  let index = 0
  return value.trim().toLocaleLowerCase('es').replace(/[\p{L}\p{M}]+(?:\.[\p{L}]+)*\.?/gu, (word) => {
    const first = index++ === 0
    if (/^d\.c\.?$/.test(word)) return 'D.C.'
    if (!first && particles.has(word)) return word
    return word.charAt(0).toLocaleUpperCase('es') + word.slice(1)
  })
}
const coStates = read('co/departamentos.json').map((d) => ({ code: d.codigo_departamento, name: titleCase(d.nombre_departamento), municipalities: [] }))
const coByCode = new Map(coStates.map((s) => [s.code, s]))
for (const m of read('co/municipios.json')) {
  assert(coByCode.has(m.cod_dpto), 'departamento del municipio ' + m.nom_mpio)
  coByCode.get(m.cod_dpto).municipalities.push({ code: m.cod_mpio, name: titleCase(m.nom_mpio) })
}
coStates.forEach((s) => s.municipalities.sort(byName))
coStates.sort(byName)
assert(coStates.length === 33, 'departamentos CO = 33')
write('geo-co.json', { countryCode: 'CO', states: coStates })

// ---------- Resumen y control de duplicados por división ----------
for (const [country, states] of [['VE', veStates], ['BR', brStates], ['CO', coStates]]) {
  let total = 0
  for (const s of states) {
    total += s.municipalities.length
    const seen = new Set()
    for (const m of s.municipalities) {
      const key = m.name.toLocaleLowerCase('es')
      assert(!seen.has(key), `municipio duplicado ${country}/${s.name}/${m.name}`)
      seen.add(key)
      assert(m.name.length <= 150 && (m.code ?? '').length <= 20, 'longitudes ' + m.name)
    }
  }
  console.log(`${country}: ${states.length} divisiones, ${total} municipios`)
}
console.log('Países:', countries.length)

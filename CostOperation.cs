
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Policy;
using System.Text;


namespace BD.Standard.OFM.OperationPlugin
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("销售出库、退货计算成本与税")]
    public class CostOperation : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            List<Field> file = this.BusinessInfo.GetFieldList();
            foreach (Field item in file)
            {
                e.FieldKeys.Add(item.Key);
            }
        }


        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            StringBuilder exjson = new StringBuilder();
            try
            {
                IOperationResult operationResult = new OperationResult();
                
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    //获取当前表单fid,实体
                    string fid = entity[0].ToString();
                    DynamicObjectType types = entity.DynamicObjectType;
                    string formid = types.Name;
                    DynamicObjectCollection entrys = null;
                    string EntryTable="";
                    string CostAmountStr= "CostAmount_LC";
                    if (formid.Equals("SAL_OUTSTOCK"))
                    {
                        entrys = entity["SAL_OUTSTOCKENTRY"] as DynamicObjectCollection;
                        EntryTable = "T_SAL_OUTSTOCKENTRY";
                    }
                    else if(formid.Equals("SAL_RETURNSTOCK"))
                    {
                        entrys = entity["SAL_RETURNSTOCKENTRY"] as DynamicObjectCollection;
                        EntryTable = "T_SAL_RETURNSTOCKENTRY";
                    }
                         
                    foreach (var entry in entrys)
                    {
                        Decimal CostAmount = (Decimal)entry[CostAmountStr];
                        if (CostAmount == 0) continue;
                        int fentryid = Convert.ToInt32(entry["Id"]);

                        DynamicObject MaterialID = (DynamicObject)entry["MaterialID"];
                        Decimal F_XCQH_gssl = (Decimal)MaterialID["F_XCQH_gssl"];
                        Decimal F_XCQH_XFSL = (Decimal)MaterialID["F_XCQH_XFSL"];
                        Decimal F_XCQH_YF = (Decimal)MaterialID["F_XCQH_YF"];

                        decimal YF= CostAmount / (1 + F_XCQH_YF) * F_XCQH_YF;

                        decimal XFSL = (CostAmount - YF) * F_XCQH_XFSL;

                        decimal gssl = (CostAmount - YF - XFSL) / (1 + F_XCQH_gssl) * F_XCQH_gssl;

                        decimal CGCB = CostAmount - YF - XFSL - gssl;

                        DBUtils.Execute(this.Context, $"update {EntryTable} set F_XCQH_YF={YF},F_XCQH_XFSL={XFSL},F_XCQH_gssl={gssl},F_XCQH_CGCB={CGCB} where fentryid={fentryid} ");

                    }

                }
                this.OperationResult.MergeResult(operationResult);

            }
            catch (Exception ex)
            {
                throw new KDException("异常信息：", ex.Message + "\r\n异常调用栈信息：" + ex.StackTrace + "\r\n数据:" + exjson);
            }

        }

       
    }
}
